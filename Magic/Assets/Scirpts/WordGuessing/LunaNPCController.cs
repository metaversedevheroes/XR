using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;

public class LunaNPCController : MonoBehaviour
{
    public static LunaNPCController Instance { get; private set; }
    
    [System.Serializable]
    public class LunaSettings
    {
        [Header("AI Configuration")]
        public string llamaModelPath = "Assets/StreamingAssets/AI/llama-3.2-1b.gguf";
        public int maxTokens = 150;
        public float temperature = 0.7f;
        public int contextWindow = 2048;
        public bool useLocalInference = true;
        
        [Header("Luna Personality")]
        public string personalityPrompt = "You are Luna, a helpful AI companion in a VR word guessing game. You think of English words and give yes/no answers to help players learn. Be encouraging, educational, and fun. Keep responses brief for VR comfort.";
        public float responseDelay = 1.5f;
        public bool enablePersonalityVariation = true;
        
        [Header("Game Integration")]
        public bool autoSelectWords = true;
        public bool adaptDifficulty = true;
        public int maxQuestionsPerWord = 10;
        public float confidenceThreshold = 0.6f;
    }
    
    [System.Serializable]
    public class LunaState
    {
        public string currentWord = "";
        public List<string> questionsAsked = new List<string>();
        public List<string> answersGiven = new List<string>();
        public int currentQuestionCount = 0;
        public bool isThinking = false;
        public bool isReady = false;
        public string currentContext = "";
        public DifficultyLevel adaptiveLevel = DifficultyLevel.Easy;
    }
    
    [Header("Luna Configuration")]
    [SerializeField] private LunaSettings settings = new LunaSettings();
    [SerializeField] private LunaState state = new LunaState();
    [SerializeField] private bool debugMode = true;
    
    [Header("Visual Components")]
    [SerializeField] private GameObject lunaAvatar;
    [SerializeField] private Animator lunaAnimator;
    [SerializeField] private Transform lunaPosition;
    [SerializeField] private GameObject thinkingIndicator;
    
    private LlamaInferenceEngine inferenceEngine;
    private WordDatabase wordDatabase;
    private Coroutine thinkingCoroutine;
    
    public static event Action<string> OnLunaResponse;
    public static event Action<string> OnLunaWordSelected;
    public static event Action<bool> OnLunaThinkingChanged;
    public static event Action<string> OnLunaStateChanged;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeLuna();
    }
    
    private async void InitializeLuna()
    {
        if (debugMode) Debug.Log("[Luna] Initializing Luna NPC Controller");
        
        try
        {
            inferenceEngine = gameObject.AddComponent<LlamaInferenceEngine>();
            await inferenceEngine.Initialize(settings.llamaModelPath, settings);
            
            wordDatabase = FindFirstObjectByType<WordDatabase>();
            if (wordDatabase == null)
            {
                Debug.LogError("[Luna] WordDatabase not found! Luna cannot function without it.");
                return;
            }
            
            SetupLunaAvatar();
            state.isReady = true;
            
            if (debugMode) Debug.Log("[Luna] Luna is ready for interaction");
            OnLunaStateChanged?.Invoke("Ready");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Luna] Failed to initialize: {e.Message}");
            OnLunaStateChanged?.Invoke("Error");
        }
    }
    
    private void SetupLunaAvatar()
    {
        if (lunaAvatar == null)
        {
            lunaAvatar = new GameObject("LunaAvatar");
            lunaAvatar.transform.SetParent(transform);
            lunaAvatar.transform.localPosition = Vector3.zero;
        }
        
        if (lunaAnimator == null && lunaAvatar != null)
        {
            lunaAnimator = lunaAvatar.GetComponent<Animator>();
        }
        
        if (thinkingIndicator != null)
        {
            thinkingIndicator.SetActive(false);
        }
    }
    
    public async Task<string> SelectNewWord()
    {
        if (!state.isReady)
        {
            Debug.LogWarning("[Luna] Luna is not ready yet");
            return "";
        }
        
        SetThinkingState(true);
        
        try
        {
            string selectedWord;
            
            if (settings.autoSelectWords && wordDatabase != null)
            {
                // Let Luna intelligently select a word based on context
                string context = BuildWordSelectionContext();
                selectedWord = await RequestWordFromAI(context);
                
                // Fallback to database if AI selection fails
                if (string.IsNullOrEmpty(selectedWord))
                {
                    selectedWord = wordDatabase.GetRandomWord();
                }
            }
            else
            {
                selectedWord = wordDatabase?.GetRandomWord() ?? "apple";
            }
            
            state.currentWord = selectedWord;
            state.questionsAsked.Clear();
            state.answersGiven.Clear();
            state.currentQuestionCount = 0;
            state.currentContext = BuildGameContext();
            
            if (debugMode) Debug.Log($"[Luna] Selected word: {selectedWord}");
            
            OnLunaWordSelected?.Invoke(selectedWord);
            return selectedWord;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Luna] Error selecting word: {e.Message}");
            return wordDatabase?.GetRandomWord() ?? "apple";
        }
        finally
        {
            SetThinkingState(false);
        }
    }
    
    public async Task<bool> ProcessQuestion(string question)
    {
        if (!state.isReady || string.IsNullOrEmpty(state.currentWord))
        {
            Debug.LogWarning("[Luna] Luna is not ready or no word is selected");
            return false;
        }
        
        SetThinkingState(true);
        
        try
        {
            string cleanQuestion = CleanAndValidateQuestion(question);
            if (string.IsNullOrEmpty(cleanQuestion))
            {
                if (debugMode) Debug.Log("[Luna] Invalid question format");
                return false;
            }
            
            state.questionsAsked.Add(cleanQuestion);
            state.currentQuestionCount++;
            
            // Build context for AI reasoning
            string context = BuildQuestionContext(cleanQuestion);
            
            // Get AI response
            bool answer = await GetAIResponse(context, cleanQuestion);
            
            string responseText = answer ? "Yes" : "No";
            state.answersGiven.Add(responseText);
            
            // Update game context for learning
            state.currentContext = BuildGameContext();
            
            if (debugMode) Debug.Log($"[Luna] Q: {cleanQuestion} | A: {responseText}");
            
            OnLunaResponse?.Invoke(responseText);
            
            // Trigger automatic stone pressing
            TriggerStonePress(answer);
            
            return answer;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Luna] Error processing question: {e.Message}");
            return false;
        }
        finally
        {
            SetThinkingState(false);
        }
    }
    
    private async Task<string> RequestWordFromAI(string context)
    {
        if (inferenceEngine == null) return "";
        
        string prompt = $"{settings.personalityPrompt}\n\n{context}\n\nSelect an appropriate English word for the current difficulty level. Respond with only the word, nothing else.";
        
        string response = await inferenceEngine.GenerateResponse(prompt);
        
        // Extract single word from response
        string[] words = response.Split(' ', '\n', '\r', '\t');
        foreach (string word in words)
        {
            string cleanWord = word.Trim().ToLower();
            if (!string.IsNullOrEmpty(cleanWord) && IsValidEnglishWord(cleanWord))
            {
                return cleanWord;
            }
        }
        
        return "";
    }
    
    private async Task<bool> GetAIResponse(string context, string question)
    {
        if (inferenceEngine == null) return UnityEngine.Random.value > 0.5f;
        
        string prompt = $"{settings.personalityPrompt}\n\n{context}\n\nQuestion: {question}\n\nAnswer yes or no based on whether the question is true about the word '{state.currentWord}'. Be accurate and helpful for learning. Respond with only 'yes' or 'no'.";
        
        string response = await inferenceEngine.GenerateResponse(prompt);
        
        // Parse yes/no response
        response = response.ToLower().Trim();
        
        if (response.Contains("yes") || response.Contains("true"))
            return true;
        else if (response.Contains("no") || response.Contains("false"))
            return false;
        else
            return AnalyzeQuestionLogically(question); // Fallback logic
    }
    
    private bool AnalyzeQuestionLogically(string question)
    {
        // Fallback logical analysis if AI response is unclear
        question = question.ToLower();
        string word = state.currentWord.ToLower();
        
        // Basic logical rules for common question types
        if (question.Contains("letter"))
        {
            foreach (char c in "abcdefghijklmnopqrstuvwxyz")
            {
                if (question.Contains(c.ToString()) && word.Contains(c))
                    return true;
            }
            return false;
        }
        
        if (question.Contains("vowel"))
        {
            return word.Any(c => "aeiou".Contains(c));
        }
        
        if (question.Contains("consonant"))
        {
            return word.Any(c => !"aeiou".Contains(c) && char.IsLetter(c));
        }
        
        // Default probabilistic response
        return UnityEngine.Random.value > 0.5f;
    }
    
    private string BuildWordSelectionContext()
    {
        return $"Current difficulty: {state.adaptiveLevel}\n" +
               $"Questions answered this session: {state.questionsAsked.Count}\n" +
               $"Context: VR English learning game for middle school students\n" +
               $"Previous words: {string.Join(", ", state.questionsAsked.Take(5))}";
    }
    
    private string BuildQuestionContext(string question)
    {
        return $"Current word: {state.currentWord}\n" +
               $"Question #{state.currentQuestionCount}: {question}\n" +
               $"Previous Q&A:\n{string.Join("\n", state.questionsAsked.Zip(state.answersGiven, (q, a) => $"Q: {q} | A: {a}"))}\n" +
               $"Context: Help the player learn about the word through yes/no questions.";
    }
    
    private string BuildGameContext()
    {
        return $"Word: {state.currentWord}, Questions: {state.currentQuestionCount}/{settings.maxQuestionsPerWord}";
    }
    
    private string CleanAndValidateQuestion(string question)
    {
        if (string.IsNullOrEmpty(question)) return "";
        
        question = question.Trim();
        
        // Basic validation for yes/no questions
        if (question.Length < 3 || question.Length > 200) return "";
        
        // Check for question indicators
        string lowerQuestion = question.ToLower();
        if (!lowerQuestion.StartsWith("is") && !lowerQuestion.StartsWith("does") && 
            !lowerQuestion.StartsWith("can") && !lowerQuestion.StartsWith("has") &&
            !lowerQuestion.Contains("?"))
        {
            return "";
        }
        
        return question;
    }
    
    private bool IsValidEnglishWord(string word)
    {
        if (string.IsNullOrEmpty(word) || word.Length < 2 || word.Length > 20)
            return false;
        
        // Basic validation - only letters
        return word.All(char.IsLetter);
    }
    
    private void TriggerStonePress(bool answer)
    {
        // Find and trigger the appropriate stone
        FeedbackStoneInteraction[] stones = FindObjectsByType<FeedbackStoneInteraction>(FindObjectsSortMode.None);
        
        foreach (var stone in stones)
        {
            if ((answer && stone.GetStoneType() == FeedbackStoneInteraction.StoneType.Blue) ||
                (!answer && stone.GetStoneType() == FeedbackStoneInteraction.StoneType.Red))
            {
                StartCoroutine(DelayedStonePress(stone, settings.responseDelay));
                break;
            }
        }
    }
    
    private IEnumerator DelayedStonePress(FeedbackStoneInteraction stone, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (stone != null)
        {
            stone.SimulatePress();
            if (debugMode) Debug.Log($"[Luna] Automatically pressed {stone.GetStoneType()} stone");
        }
    }
    
    private void SetThinkingState(bool thinking)
    {
        state.isThinking = thinking;
        
        if (thinkingIndicator != null)
            thinkingIndicator.SetActive(thinking);
        
        if (lunaAnimator != null)
            lunaAnimator.SetBool("IsThinking", thinking);
        
        OnLunaThinkingChanged?.Invoke(thinking);
        
        if (thinking && thinkingCoroutine == null)
        {
            thinkingCoroutine = StartCoroutine(ThinkingAnimation());
        }
        else if (!thinking && thinkingCoroutine != null)
        {
            StopCoroutine(thinkingCoroutine);
            thinkingCoroutine = null;
        }
    }
    
    private IEnumerator ThinkingAnimation()
    {
        while (state.isThinking)
        {
            if (debugMode) Debug.Log("[Luna] Luna is thinking...");
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    public void UpdateDifficulty(DifficultyLevel newLevel)
    {
        state.adaptiveLevel = newLevel;
        if (debugMode) Debug.Log($"[Luna] Difficulty updated to: {newLevel}");
    }
    
    public LunaState GetCurrentState()
    {
        return state;
    }
    
    public bool IsReady()
    {
        return state.isReady && !state.isThinking;
    }
    
    public void ResetForNewGame()
    {
        state.currentWord = "";
        state.questionsAsked.Clear();
        state.answersGiven.Clear();
        state.currentQuestionCount = 0;
        state.currentContext = "";
        SetThinkingState(false);
        
        if (debugMode) Debug.Log("[Luna] Reset for new game");
    }
    
    private void OnDestroy()
    {
        if (inferenceEngine != null)
        {
            inferenceEngine.Cleanup();
        }
    }
}