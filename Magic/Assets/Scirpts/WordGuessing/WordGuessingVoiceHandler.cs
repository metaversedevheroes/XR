using UnityEngine;
using System;

public class WordGuessingVoiceHandler : MonoBehaviour
{
    public static WordGuessingVoiceHandler Instance { get; private set; }
    
    [Header("Voice Recognition Integration")]
    [SerializeField] private VoiceRecognitionManager voiceRecognitionManager;
    [SerializeField] private bool autoFindVoiceManager = true;
    
    [Header("Voice Processing Settings")]
    [SerializeField] private float minimumConfidence = 0.7f;
    [SerializeField] private bool filterByRole = true;
    [SerializeField] private float voiceTimeout = 10f;
    
    [Header("Magic Book Integration")]
    [SerializeField] private int activePlayerId = -1;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool logVoiceResults = true;
    
    public static event Action<int, string, float> OnVoiceRecognized;
    public static event Action<int, string> OnQuestionAsked;
    public static event Action<int, string> OnGuessAttempted;
    public static event Action<int> OnVoiceActivated;
    public static event Action<int> OnVoiceDeactivated;
    
    private bool isListening = false;
    private float lastVoiceTime = 0f;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeVoiceHandler();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void Start()
    {
        if (autoFindVoiceManager && voiceRecognitionManager == null)
        {
            voiceRecognitionManager = FindFirstObjectByType<VoiceRecognitionManager>();
            
            if (voiceRecognitionManager == null)
            {
                Debug.LogWarning("VoiceRecognitionManager not found! Voice features will be disabled.");
                return;
            }
        }
        
        SetupVoiceCallbacks();
    }
    
    private void OnEnable()
    {
        PlayerRoleManager.OnPlayerPermissionsChanged += HandlePlayerPermissionsChanged;
        WordGuessingGameManager.OnGamePhaseChanged += HandleGamePhaseChanged;
    }
    
    private void OnDisable()
    {
        PlayerRoleManager.OnPlayerPermissionsChanged -= HandlePlayerPermissionsChanged;
        WordGuessingGameManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
    }
    
    private void InitializeVoiceHandler()
    {
        lastVoiceTime = Time.time;
        
        if (debugMode)
        {
            Debug.Log("WordGuessingVoiceHandler initialized");
        }
    }
    
    private void SetupVoiceCallbacks()
    {
        if (voiceRecognitionManager == null) return;
        
        // Subscribe to existing voice recognition events if available
        // Note: Adjust these based on your actual VoiceRecognitionManager implementation
        
        if (debugMode)
        {
            Debug.Log("Voice callbacks setup completed");
        }
    }
    
    public void ActivateVoiceForPlayer(int playerId)
    {
        if (!CanPlayerUseVoice(playerId))
        {
            if (debugMode) Debug.Log($"Player {playerId} cannot use voice (role restriction)");
            return;
        }
        
        activePlayerId = playerId;
        isListening = true;
        lastVoiceTime = Time.time;
        
        if (voiceRecognitionManager != null)
        {
            // Start voice recognition
            // Note: Adjust based on your VoiceRecognitionManager API
            voiceRecognitionManager.enabled = true;
        }
        
        OnVoiceActivated?.Invoke(playerId);
        
        if (debugMode)
        {
            Debug.Log($"Voice activated for Player {playerId}");
        }
    }
    
    public void DeactivateVoiceForPlayer(int playerId)
    {
        if (activePlayerId != playerId) return;
        
        activePlayerId = -1;
        isListening = false;
        
        if (voiceRecognitionManager != null)
        {
            // Stop voice recognition
            voiceRecognitionManager.enabled = false;
        }
        
        OnVoiceDeactivated?.Invoke(playerId);
        
        if (debugMode)
        {
            Debug.Log($"Voice deactivated for Player {playerId}");
        }
    }
    
    public void ProcessVoiceInput(string recognizedText, float confidence)
    {
        if (!isListening || activePlayerId == -1)
        {
            if (debugMode) Debug.Log("Voice input ignored - not listening or no active player");
            return;
        }
        
        if (confidence < minimumConfidence)
        {
            if (debugMode) Debug.Log($"Voice input ignored - low confidence: {confidence}");
            return;
        }
        
        lastVoiceTime = Time.time;
        
        if (logVoiceResults)
        {
            Debug.Log($"Voice input from Player {activePlayerId}: '{recognizedText}' (confidence: {confidence})");
        }
        
        OnVoiceRecognized?.Invoke(activePlayerId, recognizedText, confidence);
        
        // Determine if this is a question or a guess attempt
        if (IsGuessAttempt(recognizedText))
        {
            ProcessGuessAttempt(activePlayerId, recognizedText);
        }
        else
        {
            ProcessQuestion(activePlayerId, recognizedText);
        }
    }
    
    private bool IsGuessAttempt(string text)
    {
        // Simple heuristic to determine if the input is a single word guess
        string[] words = text.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        
        // If it's a single word, consider it a guess attempt
        if (words.Length == 1)
            return true;
        
        // Check for guess-indicating phrases
        string lowerText = text.ToLowerInvariant();
        if (lowerText.StartsWith("is it ") || 
            lowerText.StartsWith("the answer is ") ||
            lowerText.StartsWith("i think it's ") ||
            lowerText.StartsWith("it's "))
        {
            return true;
        }
        
        return false;
    }
    
    private void ProcessQuestion(int playerId, string question)
    {
        WordGuessingGameManager.GamePhase currentPhase = WordGuessingGameManager.Instance.GetCurrentPhase();
        
        if (currentPhase != WordGuessingGameManager.GamePhase.QuestionPhase)
        {
            if (debugMode) Debug.Log("Question ignored - not in question phase");
            return;
        }
        
        OnQuestionAsked?.Invoke(playerId, question);
        
        if (debugMode)
        {
            Debug.Log($"Question from Player {playerId}: '{question}'");
        }
    }
    
    private void ProcessGuessAttempt(int playerId, string guess)
    {
        // Extract the actual word from guess phrases
        string extractedWord = ExtractWordFromGuess(guess);
        
        OnGuessAttempted?.Invoke(playerId, extractedWord);
        
        // Set game phase to guess phase if not already
        if (WordGuessingGameManager.Instance.GetCurrentPhase() != WordGuessingGameManager.GamePhase.GuessPhase)
        {
            WordGuessingGameManager.Instance.SetGamePhase(WordGuessingGameManager.GamePhase.GuessPhase);
        }
        
        // Process the guess through the game manager
        WordGuessingGameManager.Instance.ProcessGuess(extractedWord);
        
        if (debugMode)
        {
            Debug.Log($"Guess attempt from Player {playerId}: '{extractedWord}' (original: '{guess}')");
        }
    }
    
    private string ExtractWordFromGuess(string guess)
    {
        string lowerGuess = guess.ToLowerInvariant().Trim();
        
        // Remove common guess prefixes
        if (lowerGuess.StartsWith("is it "))
            return lowerGuess.Substring(6).Trim();
        if (lowerGuess.StartsWith("the answer is "))
            return lowerGuess.Substring(14).Trim();
        if (lowerGuess.StartsWith("i think it's "))
            return lowerGuess.Substring(13).Trim();
        if (lowerGuess.StartsWith("it's "))
            return lowerGuess.Substring(5).Trim();
        
        // If it's already a single word, return as is
        string[] words = lowerGuess.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 1)
            return words[0];
        
        // For multi-word phrases, try to extract the last meaningful word
        return words[words.Length - 1];
    }
    
    private bool CanPlayerUseVoice(int playerId)
    {
        if (!filterByRole) return true;
        
        if (PlayerRoleManager.Instance == null) return true;
        
        return PlayerRoleManager.Instance.CanPlayerUseMagicBook(playerId);
    }
    
    private void HandlePlayerPermissionsChanged(int playerId, bool isConnected)
    {
        if (!isConnected && activePlayerId == playerId)
        {
            DeactivateVoiceForPlayer(playerId);
        }
    }
    
    private void HandleGamePhaseChanged(WordGuessingGameManager.GamePhase newPhase)
    {
        // Auto-deactivate voice during certain phases
        if (newPhase == WordGuessingGameManager.GamePhase.RoundEnd ||
            newPhase == WordGuessingGameManager.GamePhase.StageComplete ||
            newPhase == WordGuessingGameManager.GamePhase.WaitingForPlayers)
        {
            if (activePlayerId != -1)
            {
                DeactivateVoiceForPlayer(activePlayerId);
            }
        }
    }
    
    private void Update()
    {
        // Auto-timeout voice if no input for too long
        if (isListening && Time.time - lastVoiceTime > voiceTimeout)
        {
            if (debugMode) Debug.Log("Voice timed out");
            if (activePlayerId != -1)
            {
                DeactivateVoiceForPlayer(activePlayerId);
            }
        }
    }
    
    public void SetMinimumConfidence(float confidence)
    {
        minimumConfidence = Mathf.Clamp01(confidence);
        if (debugMode) Debug.Log($"Minimum confidence set to: {minimumConfidence}");
    }
    
    public void SetVoiceTimeout(float timeout)
    {
        voiceTimeout = Mathf.Max(1f, timeout);
        if (debugMode) Debug.Log($"Voice timeout set to: {voiceTimeout} seconds");
    }
    
    public bool IsVoiceActive()
    {
        return isListening && activePlayerId != -1;
    }
    
    public int GetActivePlayerId()
    {
        return activePlayerId;
    }
    
    public void SetVoiceRecognitionManager(VoiceRecognitionManager manager)
    {
        voiceRecognitionManager = manager;
        SetupVoiceCallbacks();
        
        if (debugMode)
        {
            Debug.Log("VoiceRecognitionManager reference updated");
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}