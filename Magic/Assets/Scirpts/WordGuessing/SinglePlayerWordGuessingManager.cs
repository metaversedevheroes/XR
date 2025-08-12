using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class SinglePlayerWordGuessingManager : MonoBehaviour
{
    public static SinglePlayerWordGuessingManager Instance { get; private set; }
    
    [System.Serializable]
    public class GameState
    {
        public PlayerRole playerRole = PlayerRole.Guesser;
        public PlayerRole lunaRole = PlayerRole.Describer;
        public int playerScore = 0;
        public int currentRound = 1;
        public GamePhase currentPhase = GamePhase.WaitingForStart;
        public string currentWord = "";
        public bool isGameActive = false;
        public int wordsGuessedThisStage = 0;
        public int requiredWordsPerStage = 4;
        public float sessionStartTime = 0f;
        public bool lunaReady = false;
    }
    
    public enum PlayerRole
    {
        Guesser,
        Describer
    }
    
    public enum GamePhase
    {
        WaitingForStart,
        Initialize,
        RoundStart,
        QuestionPhase,
        FeedbackPhase,
        GuessPhase,
        RoundEnd,
        StageComplete,
        GameComplete
    }
    
    [Header("Game Configuration")]
    [SerializeField] private GameState gameState = new GameState();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool autoStartGame = true;
    [SerializeField] private float gameSessionTimeLimit = 1800f; // 30 minutes
    
    [Header("Luna Integration")]
    [SerializeField] private LunaNPCController lunaController;
    [SerializeField] private bool autoFindLuna = true;
    [SerializeField] private float lunaThinkingDelay = 1.5f;
    
    [Header("Room Management")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private Transform room1Center; // Guesser room
    [SerializeField] private Transform room2Center; // Luna's room
    [SerializeField] private bool preserveTwoRoomStructure = true;
    
    [Header("Voice Integration")]
    [SerializeField] private VoiceRecognitionManager voiceRecognition;
    [SerializeField] private bool autoFindVoiceRecognition = true;
    [SerializeField] private float voiceTimeoutDuration = 15f;
    
    [Header("Components")]
    [SerializeField] private WordDatabase wordDatabase;
    [SerializeField] private AnswerValidator answerValidator;
    [SerializeField] private bool autoFindComponents = true;
    
    public static event Action<GamePhase> OnGamePhaseChanged;
    public static event Action<string> OnNewWordSelected;
    public static event Action<int> OnScoreUpdated;
    public static event Action OnStageComplete;
    public static event Action<string> OnGameMessage;
    public static event Action<string> OnQuestionAsked;
    public static event Action<bool> OnFeedbackReceived;
    
    private Coroutine gameLoopCoroutine;
    private string lastPlayerQuestion = "";
    private bool waitingForFeedback = false;
    
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
    }
    
    private void Start()
    {
        InitializeGame();
        
        if (autoStartGame)
        {
            StartCoroutine(AutoStartGameWhenReady());
        }
    }
    
    private void InitializeGame()
    {
        if (debugMode) Debug.Log("[SinglePlayerWG] Initializing single-player word guessing game");
        
        FindComponents();
        SetupEventListeners();
        
        gameState.sessionStartTime = Time.time;
        gameState.currentPhase = GamePhase.Initialize;
        OnGamePhaseChanged?.Invoke(gameState.currentPhase);
        
        InitializePlayerRoom();
    }
    
    private void FindComponents()
    {
        if (autoFindComponents)
        {
            if (wordDatabase == null)
                wordDatabase = FindFirstObjectByType<WordDatabase>();
                
            if (answerValidator == null)
                answerValidator = FindFirstObjectByType<AnswerValidator>();
        }
        
        if (autoFindLuna && lunaController == null)
        {
            lunaController = FindFirstObjectByType<LunaNPCController>();
            if (lunaController == null)
            {
                Debug.Log("[SinglePlayerWG] Luna NPC Controller not found. Auto-creating one...");
                GameObject lunaObj = new GameObject("LunaNPCController");
                lunaController = lunaObj.AddComponent<LunaNPCController>();
            }
        }
        
        if (autoFindVoiceRecognition && voiceRecognition == null)
        {
            voiceRecognition = FindFirstObjectByType<VoiceRecognitionManager>();
        }
    }
    
    private void SetupEventListeners()
    {
        if (lunaController != null)
        {
            LunaNPCController.OnLunaResponse += HandleLunaResponse;
            LunaNPCController.OnLunaWordSelected += HandleLunaWordSelection;
            LunaNPCController.OnLunaStateChanged += HandleLunaStateChanged;
        }
        
        // Connect to voice recognition events
        VoiceRecognitionEvents.OnVoiceRecognized += HandleVoiceRecognized;
        
        if (voiceRecognition != null)
        {
            if (debugMode) Debug.Log("[SinglePlayerWG] Voice recognition connected successfully");
        }
    }
    
    private void InitializePlayerRoom()
    {
        // Always start player as Guesser in Room 1
        gameState.playerRole = PlayerRole.Guesser;
        gameState.lunaRole = PlayerRole.Describer;
        
        if (preserveTwoRoomStructure)
        {
            // Position player in Room 1 (Guesser's room)
            if (playerSpawnPoint != null)
            {
                var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    xrOrigin.transform.position = playerSpawnPoint.position;
                    xrOrigin.transform.rotation = playerSpawnPoint.rotation;
                }
            }
        }
        
        if (debugMode) Debug.Log($"[SinglePlayerWG] Player initialized as {gameState.playerRole} in Room 1");
    }
    
    private IEnumerator AutoStartGameWhenReady()
    {
        // Wait for all components to be ready
        while (!IsSystemReady())
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        yield return new WaitForSeconds(1f); // Brief pause
        StartNewGame();
    }
    
    private bool IsSystemReady()
    {
        bool ready = wordDatabase != null && 
                    lunaController != null && 
                    lunaController.IsReady();
        
        if (!ready && debugMode)
        {
            Debug.Log("[SinglePlayerWG] Waiting for system components to be ready...");
        }
        
        return ready;
    }
    
    public void StartNewGame()
    {
        if (!IsSystemReady())
        {
            Debug.LogWarning("[SinglePlayerWG] Cannot start game - system not ready");
            return;
        }
        
        if (debugMode) Debug.Log("[SinglePlayerWG] Starting new single-player game");
        
        gameState.isGameActive = true;
        gameState.currentRound = 1;
        gameState.playerScore = 0;
        gameState.wordsGuessedThisStage = 0;
        gameState.currentPhase = GamePhase.RoundStart;
        
        OnGamePhaseChanged?.Invoke(gameState.currentPhase);
        OnGameMessage?.Invoke("Welcome to the Word Guessing Challenge with Luna!");
        
        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
        }
        
        gameLoopCoroutine = StartCoroutine(GameLoop());
    }
    
    private IEnumerator GameLoop()
    {
        while (gameState.isGameActive)
        {
            switch (gameState.currentPhase)
            {
                case GamePhase.RoundStart:
                    yield return StartCoroutine(HandleRoundStart());
                    break;
                    
                case GamePhase.QuestionPhase:
                    yield return StartCoroutine(HandleQuestionPhase());
                    break;
                    
                case GamePhase.FeedbackPhase:
                    yield return StartCoroutine(HandleFeedbackPhase());
                    break;
                    
                case GamePhase.GuessPhase:
                    yield return StartCoroutine(HandleGuessPhase());
                    break;
                    
                case GamePhase.RoundEnd:
                    yield return StartCoroutine(HandleRoundEnd());
                    break;
                    
                case GamePhase.StageComplete:
                    yield return StartCoroutine(HandleStageComplete());
                    break;
                    
                case GamePhase.GameComplete:
                    yield return StartCoroutine(HandleGameComplete());
                    break;
                    
                default:
                    yield return new WaitForSeconds(0.1f);
                    break;
            }
            
            // Check for session timeout
            if (Time.time - gameState.sessionStartTime > gameSessionTimeLimit)
            {
                EndGame("Session time limit reached");
                break;
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private IEnumerator HandleRoundStart()
    {
        if (debugMode) Debug.Log($"[SinglePlayerWG] Starting Round {gameState.currentRound}");
        
        // Get Luna to select a new word
        OnGameMessage?.Invoke($"Round {gameState.currentRound}: Luna is thinking of a word...");
        
        if (lunaController != null)
        {
            // Start the async task
            var wordTask = lunaController.SelectNewWord();
            
            // Wait for completion
            yield return new WaitUntil(() => wordTask.IsCompleted);
            
            string selectedWord = wordTask.Result;
            if (!string.IsNullOrEmpty(selectedWord))
            {
                gameState.currentWord = selectedWord;
                OnNewWordSelected?.Invoke(selectedWord);
                
                if (debugMode) Debug.Log($"[SinglePlayerWG] Word selected: {selectedWord}");
            }
            else
            {
                Debug.LogError("[SinglePlayerWG] Luna failed to select a word!");
                yield break;
            }
        }
        
        yield return new WaitForSeconds(lunaThinkingDelay);
        
        OnGameMessage?.Invoke("Ask yes/no questions to guess the word! Place your hand on the magic book to speak.");
        
        SetGamePhase(GamePhase.QuestionPhase);
    }
    
    private IEnumerator HandleQuestionPhase()
    {
        waitingForFeedback = false;
        
        // Voice recognition is already handled by VoiceRecognitionEvents
        if (voiceRecognition != null && debugMode)
        {
            Debug.Log("[SinglePlayerWG] Ready for voice input during question phase");
        }
        
        // Wait for player to ask a question or attempt a guess
        float questionTimeout = voiceTimeoutDuration;
        float timer = 0f;
        
        while (string.IsNullOrEmpty(lastPlayerQuestion) && timer < questionTimeout)
        {
            timer += Time.deltaTime;
            yield return null;
        }
        
        if (!string.IsNullOrEmpty(lastPlayerQuestion))
        {
            OnQuestionAsked?.Invoke(lastPlayerQuestion);
            
            // Check if it's a guess attempt (single word) or a question
            if (IsGuessAttempt(lastPlayerQuestion))
            {
                SetGamePhase(GamePhase.GuessPhase);
            }
            else
            {
                SetGamePhase(GamePhase.FeedbackPhase);
            }
        }
        else
        {
            OnGameMessage?.Invoke("No question detected. Try again!");
            yield return new WaitForSeconds(1f);
        }
    }
    
    private IEnumerator HandleFeedbackPhase()
    {
        if (string.IsNullOrEmpty(lastPlayerQuestion))
        {
            SetGamePhase(GamePhase.QuestionPhase);
            yield break;
        }
        
        OnGameMessage?.Invoke("Luna is thinking about your question...");
        waitingForFeedback = true;
        
        // Send question to Luna for processing
        if (lunaController != null)
        {
            // Start the async task
            var questionTask = lunaController.ProcessQuestion(lastPlayerQuestion);
            
            // Wait for completion
            yield return new WaitUntil(() => questionTask.IsCompleted);
            
            bool answer = questionTask.Result;
            
            // Luna will automatically press the stone, triggering room feedback
            yield return new WaitForSeconds(lunaThinkingDelay);
            
            OnFeedbackReceived?.Invoke(answer);
        }
        
        // Reset for next question
        lastPlayerQuestion = "";
        waitingForFeedback = false;
        
        yield return new WaitForSeconds(2f); // Show feedback for 2 seconds
        SetGamePhase(GamePhase.QuestionPhase);
    }
    
    private IEnumerator HandleGuessPhase()
    {
        if (string.IsNullOrEmpty(lastPlayerQuestion))
        {
            SetGamePhase(GamePhase.QuestionPhase);
            yield break;
        }
        
        OnGameMessage?.Invoke($"You guessed: {lastPlayerQuestion}");
        
        // Validate the guess
        bool isCorrect = false;
        if (answerValidator != null)
        {
            isCorrect = answerValidator.ValidateAnswer(lastPlayerQuestion, gameState.currentWord);
        }
        else
        {
            isCorrect = string.Equals(lastPlayerQuestion.Trim().ToLower(), 
                                    gameState.currentWord.ToLower(), 
                                    StringComparison.OrdinalIgnoreCase);
        }
        
        if (isCorrect)
        {
            // Correct guess!
            gameState.playerScore += 10;
            gameState.wordsGuessedThisStage++;
            
            OnScoreUpdated?.Invoke(gameState.playerScore);
            OnGameMessage?.Invoke($"Correct! The word was '{gameState.currentWord}'");
            
            if (debugMode) Debug.Log($"[SinglePlayerWG] Player guessed correctly: {gameState.currentWord}");
            
            yield return new WaitForSeconds(3f);
            SetGamePhase(GamePhase.RoundEnd);
        }
        else
        {
            // Incorrect guess
            OnGameMessage?.Invoke($"Not quite right. Keep asking questions!");
            
            if (debugMode) Debug.Log($"[SinglePlayerWG] Incorrect guess: {lastPlayerQuestion} (word: {gameState.currentWord})");
            
            yield return new WaitForSeconds(2f);
            lastPlayerQuestion = "";
            SetGamePhase(GamePhase.QuestionPhase);
        }
    }
    
    private IEnumerator HandleRoundEnd()
    {
        gameState.currentRound++;
        lastPlayerQuestion = "";
        
        if (gameState.wordsGuessedThisStage >= gameState.requiredWordsPerStage)
        {
            SetGamePhase(GamePhase.StageComplete);
        }
        else
        {
            yield return new WaitForSeconds(1f);
            SetGamePhase(GamePhase.RoundStart);
        }
    }
    
    private IEnumerator HandleStageComplete()
    {
        OnStageComplete?.Invoke();
        OnGameMessage?.Invoke($"Stage Complete! Score: {gameState.playerScore}");
        
        if (debugMode) Debug.Log($"[SinglePlayerWG] Stage completed with score: {gameState.playerScore}");
        
        // Reset for next stage
        gameState.wordsGuessedThisStage = 0;
        
        yield return new WaitForSeconds(3f);
        
        // Continue to next stage or end game
        if (gameState.currentRound > 10) // Arbitrary limit
        {
            SetGamePhase(GamePhase.GameComplete);
        }
        else
        {
            SetGamePhase(GamePhase.RoundStart);
        }
    }
    
    private IEnumerator HandleGameComplete()
    {
        gameState.isGameActive = false;
        OnGameMessage?.Invoke($"Game Complete! Final Score: {gameState.playerScore}");
        
        if (debugMode) Debug.Log($"[SinglePlayerWG] Game completed with final score: {gameState.playerScore}");
        
        yield return new WaitForSeconds(5f);
        
        // Option to restart
        OnGameMessage?.Invoke("Would you like to play again?");
    }
    
    private bool IsGuessAttempt(string input)
    {
        if (string.IsNullOrEmpty(input)) return false;
        
        // Simple heuristic: if it's a single word and doesn't contain question words, treat as guess
        string[] words = input.Trim().Split(' ');
        if (words.Length == 1) return true;
        
        string lowerInput = input.ToLower();
        return !lowerInput.Contains("is") && !lowerInput.Contains("does") && 
               !lowerInput.Contains("can") && !lowerInput.Contains("has") &&
               !lowerInput.Contains("?");
    }
    
    private void HandleLunaResponse(string response)
    {
        if (debugMode) Debug.Log($"[SinglePlayerWG] Luna responded: {response}");
        
        if (waitingForFeedback)
        {
            OnGameMessage?.Invoke($"Luna says: {response}");
        }
    }
    
    private void HandleLunaWordSelection(string word)
    {
        if (debugMode) Debug.Log($"[SinglePlayerWG] Luna selected word: {word}");
        gameState.currentWord = word;
    }
    
    private void HandleLunaStateChanged(string state)
    {
        gameState.lunaReady = state == "Ready";
        if (debugMode) Debug.Log($"[SinglePlayerWG] Luna state: {state}");
    }
    
    private void HandlePlayerQuestion(string question)
    {
        lastPlayerQuestion = question;
        if (debugMode) Debug.Log($"[SinglePlayerWG] Player asked: {question}");
    }
    
    private void HandlePlayerGuess(string guess)
    {
        lastPlayerQuestion = guess;
        if (debugMode) Debug.Log($"[SinglePlayerWG] Player guessed: {guess}");
    }
    
    private void HandleVoiceRecognized(string text, float confidence)
    {
        if (!gameState.isGameActive) return;
        
        if (debugMode) Debug.Log($"[SinglePlayerWG] Voice recognized: '{text}' (confidence: {confidence:F2})");
        
        // Process as question or guess based on current game phase
        if (gameState.currentPhase == GamePhase.QuestionPhase)
        {
            lastPlayerQuestion = text;
        }
        else if (gameState.currentPhase == GamePhase.GuessPhase)
        {
            lastPlayerQuestion = text;
        }
    }
    
    public void SetGamePhase(GamePhase newPhase)
    {
        if (gameState.currentPhase != newPhase)
        {
            gameState.currentPhase = newPhase;
            OnGamePhaseChanged?.Invoke(newPhase);
            
            if (debugMode) Debug.Log($"[SinglePlayerWG] Game phase changed to: {newPhase}");
        }
    }
    
    public void EndGame(string reason = "")
    {
        gameState.isGameActive = false;
        
        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
        }
        
        string message = string.IsNullOrEmpty(reason) ? 
            "Game ended." : 
            $"Game ended: {reason}";
            
        OnGameMessage?.Invoke(message);
        
        if (debugMode) Debug.Log($"[SinglePlayerWG] {message}");
    }
    
    public GameState GetCurrentState()
    {
        return gameState;
    }
    
    public GamePhase GetCurrentPhase()
    {
        return gameState.currentPhase;
    }
    
    public string GetCurrentWord()
    {
        return gameState.currentWord;
    }
    
    public int GetPlayerScore()
    {
        return gameState.playerScore;
    }
    
    public bool IsGameActive()
    {
        return gameState.isGameActive;
    }
    
    private void OnDestroy()
    {
        // Stop game loop coroutine first
        if (gameLoopCoroutine != null)
        {
            StopCoroutine(gameLoopCoroutine);
            gameLoopCoroutine = null;
        }
        
        // Clean up event listeners
        LunaNPCController.OnLunaResponse -= HandleLunaResponse;
        LunaNPCController.OnLunaWordSelected -= HandleLunaWordSelection;
        LunaNPCController.OnLunaStateChanged -= HandleLunaStateChanged;
        
        // Clean up voice recognition events
        VoiceRecognitionEvents.OnVoiceRecognized -= HandleVoiceRecognized;
        
        // End game to ensure clean state
        gameState.isGameActive = false;
        
        if (debugMode) Debug.Log("[SinglePlayerWG] SinglePlayerWordGuessingManager cleaned up");
    }
}