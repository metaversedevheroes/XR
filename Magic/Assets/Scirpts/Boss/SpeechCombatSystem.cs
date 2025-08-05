using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class SpeechCombatSystem : MonoBehaviour
{
    [System.Serializable]
    public class SpeechSettings
    {
        public float listeningTimeout = 15f;
        public float speechDetectionThreshold = 0.5f;
        public int maxRetries = 3;
        public float retryDelay = 1f;
        public bool enableHints = true;
        public bool strictMode = false;
    }
    
    public enum ListeningState
    {
        Idle,
        WaitingForSpeech,
        Processing,
        ValidatingAnswer,
        ShowingFeedback
    }
    
    [Header("Speech Configuration")]
    [SerializeField] private SpeechSettings settings = new SpeechSettings();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private int baseDamage = 25;
    [SerializeField] private int vocabularyDamage = 15;
    [SerializeField] private float speechTimeLimit = 15f;
    
    [Header("Integration")]
    [SerializeField] private VoiceRecognitionManager voiceManager;
    
    private ListeningState currentState = ListeningState.Idle;
    private EnglishSentence currentSentence;
    private List<string> currentVocabularySequence;
    private int vocabularyIndex = 0;
    private int currentRetries = 0;
    private bool isListening = false;
    private Coroutine listeningCoroutine;
    private string lastRecognizedText = "";
    
    public static event Action<int> OnCorrectAnswer;
    public static event Action OnIncorrectAnswer;
    public static event Action OnTimeExpired;
    public static event Action<string> OnSpeechRecognized;
    public static event Action<string> OnFeedbackMessage;
    public static event Action<EnglishSentence> OnNewSentencePresented;
    public static event Action<List<string>, int> OnVocabularySequenceStarted;
    
    private void Awake()
    {
        if (voiceManager == null)
        {
            voiceManager = FindObjectOfType<VoiceRecognitionManager>();
        }
    }
    
    private void Start()
    {
        InitializeSpeechSystem();
    }
    
    private void InitializeSpeechSystem()
    {
        if (voiceManager != null)
        {
            // Subscribe to voice recognition events
            // Note: This assumes the VoiceRecognitionManager has appropriate events
            if (debugMode) Debug.Log("Speech Combat System initialized with VoiceRecognitionManager");
        }
        else
        {
            if (debugMode) Debug.LogWarning("VoiceRecognitionManager not found! Creating fallback system.");
            CreateFallbackVoiceSystem();
        }
        
        SetState(ListeningState.Idle);
    }
    
    private void CreateFallbackVoiceSystem()
    {
        // Create a basic fallback system for testing
        GameObject voiceObj = new GameObject("FallbackVoiceSystem");
        voiceObj.transform.parent = transform;
        
        // Add basic audio source for feedback
        AudioSource audioSource = voiceObj.AddComponent<AudioSource>();
        
        if (debugMode) Debug.Log("Created fallback voice recognition system");
    }
    
    public void StartListening()
    {
        if (currentState != ListeningState.Idle)
        {
            StopListening();
        }
        
        isListening = true;
        SetState(ListeningState.WaitingForSpeech);
        
        if (listeningCoroutine != null)
        {
            StopCoroutine(listeningCoroutine);
        }
        
        listeningCoroutine = StartCoroutine(ListeningRoutine());
        
        if (debugMode) Debug.Log("Started listening for speech input");
    }
    
    public void StopListening()
    {
        isListening = false;
        
        if (listeningCoroutine != null)
        {
            StopCoroutine(listeningCoroutine);
            listeningCoroutine = null;
        }
        
        if (voiceManager != null)
        {
            // Stop voice recognition
        }
        
        SetState(ListeningState.Idle);
        
        if (debugMode) Debug.Log("Stopped listening for speech input");
    }
    
    private IEnumerator ListeningRoutine()
    {
        float startTime = Time.time;
        
        while (isListening && (Time.time - startTime) < speechTimeLimit)
        {
            // Simulate speech recognition or integrate with actual system
            if (voiceManager != null)
            {
                // Check for speech input from VoiceRecognitionManager
                yield return StartCoroutine(CheckForSpeechInput());
            }
            else
            {
                // Fallback: Check for keyboard input for testing
                yield return StartCoroutine(CheckForKeyboardInput());
            }
            
            yield return new WaitForSeconds(0.1f);
        }
        
        if (isListening)
        {
            HandleTimeExpired();
        }
    }
    
    private IEnumerator CheckForSpeechInput()
    {
        // This would integrate with the actual VoiceRecognitionManager
        // For now, we'll create a placeholder that can be extended
        yield return null;
        
        // Example integration point:
        // string recognizedText = voiceManager.GetLastRecognizedText();
        // if (!string.IsNullOrEmpty(recognizedText) && recognizedText != lastRecognizedText)
        // {
        //     ProcessSpeechInput(recognizedText);
        // }
    }
    
    private IEnumerator CheckForKeyboardInput()
    {
        // Fallback system for testing with keyboard
        if (Input.inputString.Length > 0)
        {
            foreach (char c in Input.inputString)
            {
                if (c == '\b') // Backspace
                {
                    if (lastRecognizedText.Length > 0)
                    {
                        lastRecognizedText = lastRecognizedText.Substring(0, lastRecognizedText.Length - 1);
                    }
                }
                else if (c == '\n' || c == '\r') // Enter
                {
                    if (!string.IsNullOrEmpty(lastRecognizedText))
                    {
                        ProcessSpeechInput(lastRecognizedText);
                        lastRecognizedText = "";
                    }
                }
                else if (char.IsLetterOrDigit(c) || char.IsWhiteSpace(c) || char.IsPunctuation(c))
                {
                    lastRecognizedText += c;
                }
            }
        }
        
        yield return null;
    }
    
    public void ProcessSpeechInput(string recognizedText)
    {
        if (!isListening || string.IsNullOrEmpty(recognizedText))
            return;
        
        SetState(ListeningState.Processing);
        OnSpeechRecognized?.Invoke(recognizedText);
        
        if (debugMode) Debug.Log($"Processing speech input: {recognizedText}");
        
        StartCoroutine(ValidateSpeechInput(recognizedText));
    }
    
    private IEnumerator ValidateSpeechInput(string input)
    {
        SetState(ListeningState.ValidatingAnswer);
        
        yield return new WaitForSeconds(0.5f); // Brief processing delay
        
        bool isCorrect = false;
        int damage = 0;
        string feedbackMessage = "";
        
        if (currentVocabularySequence != null && currentVocabularySequence.Count > 0)
        {
            // Validate vocabulary sequence
            isCorrect = ValidateVocabularyInput(input, out damage, out feedbackMessage);
        }
        else if (currentSentence != null)
        {
            // Validate sentence completion
            isCorrect = ValidateSentenceInput(input, out damage, out feedbackMessage);
        }
        
        SetState(ListeningState.ShowingFeedback);
        OnFeedbackMessage?.Invoke(feedbackMessage);
        
        yield return new WaitForSeconds(1f);
        
        if (isCorrect)
        {
            HandleCorrectAnswer(damage);
        }
        else
        {
            HandleIncorrectAnswer();
        }
    }
    
    private bool ValidateSentenceInput(string input, out int damage, out string feedback)
    {
        damage = 0;
        feedback = "";
        
        if (currentSentence == null)
        {
            feedback = "No active sentence to validate.";
            return false;
        }
        
        EnglishSentenceDatabase database = GetComponent<EnglishSentenceDatabase>();
        if (database == null)
        {
            feedback = "Sentence database not available.";
            return false;
        }
        
        // Check if user spoke the complete sentence
        if (database.ValidateCompleteSentence(input, currentSentence))
        {
            damage = baseDamage;
            feedback = $"Perfect! Complete sentence: '{currentSentence.completeSentence}'";
            return true;
        }
        
        // Check if user provided just the missing word(s)
        if (database.ValidateAnswer(input, currentSentence))
        {
            damage = baseDamage / 2; // Half damage for partial answer
            feedback = $"Correct word! Complete sentence: '{currentSentence.completeSentence}'";
            return true;
        }
        
        // Provide helpful feedback
        if (settings.enableHints && currentRetries < settings.maxRetries)
        {
            feedback = $"Try again. Hint: {currentSentence.hint}";
        }
        else
        {
            feedback = $"Incorrect. The answer was: '{currentSentence.correctAnswers[0]}'";
        }
        
        return false;
    }
    
    private bool ValidateVocabularyInput(string input, out int damage, out string feedback)
    {
        damage = 0;
        feedback = "";
        
        if (currentVocabularySequence == null || vocabularyIndex >= currentVocabularySequence.Count)
        {
            feedback = "No active vocabulary sequence.";
            return false;
        }
        
        string expectedWord = currentVocabularySequence[vocabularyIndex].ToLower();
        string userInput = input.Trim().ToLower();
        
        if (userInput.Equals(expectedWord))
        {
            vocabularyIndex++;
            damage = vocabularyDamage;
            
            if (vocabularyIndex >= currentVocabularySequence.Count)
            {
                // Sequence completed
                damage *= 2; // Bonus for completing sequence
                feedback = "Vocabulary sequence completed! Bonus damage!";
                currentVocabularySequence = null;
                vocabularyIndex = 0;
            }
            else
            {
                feedback = $"Correct! Next word: {vocabularyIndex + 1}/{currentVocabularySequence.Count}";
            }
            
            return true;
        }
        else
        {
            feedback = $"Expected '{expectedWord}', but heard '{userInput}'. Try again!";
            return false;
        }
    }
    
    private void HandleCorrectAnswer(int damage)
    {
        currentRetries = 0;
        SetState(ListeningState.Idle);
        
        OnCorrectAnswer?.Invoke(damage);
        
        if (debugMode) Debug.Log($"Correct answer! Dealing {damage} damage");
    }
    
    private void HandleIncorrectAnswer()
    {
        currentRetries++;
        
        if (currentRetries < settings.maxRetries)
        {
            // Give another chance
            StartCoroutine(RetryAfterDelay());
        }
        else
        {
            // Max retries reached
            currentRetries = 0;
            SetState(ListeningState.Idle);
            OnIncorrectAnswer?.Invoke();
            
            if (debugMode) Debug.Log("Max retries reached. Incorrect answer.");
        }
    }
    
    private IEnumerator RetryAfterDelay()
    {
        yield return new WaitForSeconds(settings.retryDelay);
        
        if (isListening)
        {
            SetState(ListeningState.WaitingForSpeech);
            OnFeedbackMessage?.Invoke("Try speaking again...");
        }
    }
    
    private void HandleTimeExpired()
    {
        currentRetries = 0;
        SetState(ListeningState.Idle);
        OnTimeExpired?.Invoke();
        
        if (debugMode) Debug.Log("Speech input time expired");
    }
    
    public void SetCurrentSentence(EnglishSentence sentence)
    {
        currentSentence = sentence;
        currentVocabularySequence = null;
        vocabularyIndex = 0;
        currentRetries = 0;
        
        OnNewSentencePresented?.Invoke(sentence);
        
        if (debugMode) Debug.Log($"New sentence set: {sentence.sentenceWithBlanks}");
    }
    
    public void SetVocabularySequence(List<string> sequence)
    {
        currentVocabularySequence = sequence;
        vocabularyIndex = 0;
        currentSentence = null;
        currentRetries = 0;
        
        OnVocabularySequenceStarted?.Invoke(sequence, vocabularyIndex);
        
        if (debugMode) Debug.Log($"New vocabulary sequence started with {sequence.Count} words");
    }
    
    public void StartVocabularyChallenge(int wordCount = 3)
    {
        EnglishSentenceDatabase database = GetComponent<EnglishSentenceDatabase>();
        if (database != null)
        {
            List<string> sequence = database.GetVocabularySequence(wordCount);
            SetVocabularySequence(sequence);
            StartListening();
        }
    }
    
    private void SetState(ListeningState newState)
    {
        currentState = newState;
        
        if (debugMode) Debug.Log($"Speech system state changed to: {newState}");
    }
    
    public ListeningState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsListening()
    {
        return isListening;
    }
    
    public EnglishSentence GetCurrentSentence()
    {
        return currentSentence;
    }
    
    public List<string> GetCurrentVocabularySequence()
    {
        return currentVocabularySequence;
    }
    
    public int getCurrentVocabularyIndex()
    {
        return vocabularyIndex;
    }
    
    public void SetSpeechTimeLimit(float timeLimit)
    {
        speechTimeLimit = timeLimit;
    }
    
    public void SetBaseDamage(int damage)
    {
        baseDamage = damage;
    }
    
    public void EnableStrictMode(bool enabled)
    {
        settings.strictMode = enabled;
    }
    
    public void EnableHints(bool enabled)
    {
        settings.enableHints = enabled;
    }
    
    private void OnDestroy()
    {
        StopListening();
    }
    
    // Method to integrate with existing VoiceRecognitionManager
    public void OnVoiceRecognitionResult(string result)
    {
        if (isListening)
        {
            ProcessSpeechInput(result);
        }
    }
    
    // Method for manual testing
    [System.Obsolete("This method is for testing only")]
    public void SimulateSpeechInput(string input)
    {
        if (debugMode)
        {
            ProcessSpeechInput(input);
        }
    }
}