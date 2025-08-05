using UnityEngine;

[System.Serializable]
public class GameSetupConfiguration
{
    [Header("Game Settings")]
    public DifficultyLevel startingDifficulty = DifficultyLevel.Easy;
    public int wordsPerStage = 4;
    public float roundTimeLimit = 300f; // 5 minutes
    public bool enableHints = false;
    
    [Header("Voice Settings")]
    public float voiceConfidenceThreshold = 0.7f;
    public float voiceTimeout = 10f;
    public bool enableVoiceDebug = true;
    
    [Header("Visual Settings")]
    public bool useAnimations = true;
    public bool enableParticleEffects = true;
    public float uiTransitionSpeed = 0.5f;
}

public class WordGuessingGameSetup : MonoBehaviour
{
    [Header("Setup Configuration")]
    [SerializeField] private GameSetupConfiguration config = new GameSetupConfiguration();
    [SerializeField] private bool autoSetupOnStart = true;
    [SerializeField] private bool validateSetup = true;
    
    [Header("Core Components")]
    [SerializeField] private WordGuessingGameManager gameManager;
    [SerializeField] private PlayerRoleManager roleManager;
    [SerializeField] private WordGuessingVoiceHandler voiceHandler;
    [SerializeField] private PlayerMessageRelay messageRelay;
    [SerializeField] private WordGuessingUIManager uiManager;
    
    [Header("Room 1 Components (Player 1 - Usually Guesser)")]
    [SerializeField] private MagicBookInteraction room1MagicBook;
    [SerializeField] private MagicBookTextDisplay room1TextDisplay;
    [SerializeField] private RoomFeedbackController room1FeedbackController;
    
    [Header("Room 2 Components (Player 2 - Usually Describer)")]
    [SerializeField] private FeedbackStoneInteraction room2BlueStone;
    [SerializeField] private FeedbackStoneInteraction room2RedStone;
    [SerializeField] private PictureFrameDisplay room2PictureFrame;
    [SerializeField] private RoomFeedbackController room2FeedbackController;
    
    [Header("Auto-Find Settings")]
    [SerializeField] private bool autoFindComponents = true;
    [SerializeField] private bool createMissingComponents = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showSetupProgress = true;
    
    private bool isSetupComplete = false;
    
    private void Awake()
    {
        if (autoSetupOnStart)
        {
            SetupGame();
        }
    }
    
    [ContextMenu("Setup Game")]
    public void SetupGame()
    {
        if (showSetupProgress) Debug.Log("Starting Word Guessing Game setup...");
        
        // Step 1: Find or create core components
        SetupCoreComponents();
        
        // Step 2: Setup room components
        SetupRoomComponents();
        
        // Step 3: Configure components
        ConfigureComponents();
        
        // Step 4: Establish connections
        EstablishConnections();
        
        // Step 5: Validate setup
        if (validateSetup)
        {
            ValidateSetup();
        }
        
        // Step 6: Initialize game
        InitializeGame();
        
        isSetupComplete = true;
        
        if (showSetupProgress) Debug.Log("Word Guessing Game setup complete!");
    }
    
    private void SetupCoreComponents()
    {
        if (showSetupProgress) Debug.Log("Setting up core components...");
        
        // Game Manager
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<WordGuessingGameManager>();
            if (gameManager == null && createMissingComponents)
            {
                GameObject gmObject = new GameObject("WordGuessingGameManager");
                gameManager = gmObject.AddComponent<WordGuessingGameManager>();
                gmObject.AddComponent<WordDatabase>();
                gmObject.AddComponent<AnswerValidator>();
            }
        }
        
        // Role Manager
        if (roleManager == null)
        {
            roleManager = FindFirstObjectByType<PlayerRoleManager>();
            if (roleManager == null && createMissingComponents)
            {
                GameObject rmObject = new GameObject("PlayerRoleManager");
                roleManager = rmObject.AddComponent<PlayerRoleManager>();
            }
        }
        
        // Voice Handler
        if (voiceHandler == null)
        {
            voiceHandler = FindFirstObjectByType<WordGuessingVoiceHandler>();
            if (voiceHandler == null && createMissingComponents)
            {
                GameObject vhObject = new GameObject("WordGuessingVoiceHandler");
                voiceHandler = vhObject.AddComponent<WordGuessingVoiceHandler>();
            }
        }
        
        // Message Relay
        if (messageRelay == null)
        {
            messageRelay = FindFirstObjectByType<PlayerMessageRelay>();
            if (messageRelay == null && createMissingComponents)
            {
                GameObject mrObject = new GameObject("PlayerMessageRelay");
                messageRelay = mrObject.AddComponent<PlayerMessageRelay>();
            }
        }
        
        // UI Manager
        if (uiManager == null)
        {
            uiManager = FindFirstObjectByType<WordGuessingUIManager>();
            if (uiManager == null && createMissingComponents)
            {
                GameObject uiObject = new GameObject("WordGuessingUIManager");
                uiManager = uiObject.AddComponent<WordGuessingUIManager>();
            }
        }
    }
    
    private void SetupRoomComponents()
    {
        if (showSetupProgress) Debug.Log("Setting up room components...");
        
        if (autoFindComponents)
        {
            // Find Room 1 components
            if (room1MagicBook == null)
                room1MagicBook = FindFirstObjectByType<MagicBookInteraction>();
            
            if (room1TextDisplay == null)
                room1TextDisplay = FindFirstObjectByType<MagicBookTextDisplay>();
            
            if (room1FeedbackController == null)
                room1FeedbackController = FindFirstObjectByType<RoomFeedbackController>();
            
            // Find Room 2 components - using FindObjectsByType to get all instances
            var stones = FindObjectsByType<FeedbackStoneInteraction>(FindObjectsSortMode.None);
            if (room2BlueStone == null && stones.Length > 0)
                room2BlueStone = stones[0];
            if (room2RedStone == null && stones.Length > 1)
                room2RedStone = stones[1];
            
            if (room2PictureFrame == null)
                room2PictureFrame = FindFirstObjectByType<PictureFrameDisplay>();
            
            if (room2FeedbackController == null)
                room2FeedbackController = FindFirstObjectByType<RoomFeedbackController>();
        }
    }
    
    private void ConfigureComponents()
    {
        if (showSetupProgress) Debug.Log("Configuring components...");
        
        // Configure Game Manager
        if (gameManager != null)
        {
            var gameState = gameManager.GetGameState();
            gameState.requiredWordsPerStage = config.wordsPerStage;
        }
        
        // Configure Voice Handler
        if (voiceHandler != null)
        {
            voiceHandler.SetMinimumConfidence(config.voiceConfidenceThreshold);
            voiceHandler.SetVoiceTimeout(config.voiceTimeout);
        }
        
        // Configure Room 1 (Player 1 - Guesser)
        if (room1MagicBook != null)
        {
            room1MagicBook.SetAssignedPlayer(1);
            if (room1TextDisplay != null)
                room1MagicBook.SetTextDisplay(room1TextDisplay);
        }
        
        if (room1TextDisplay != null)
        {
            room1TextDisplay.SetAssignedPlayer(1);
            room1TextDisplay.SetReceiveAllMessages(true);
        }
        
        if (room1FeedbackController != null)
        {
            room1FeedbackController.SetAssignedPlayer(1);
        }
        
        // Configure Room 2 (Player 2 - Describer)
        if (room2BlueStone != null)
        {
            room2BlueStone.SetStoneType(FeedbackStoneInteraction.StoneType.Blue);
            room2BlueStone.SetAssignedPlayer(2);
            if (room2FeedbackController != null)
                room2BlueStone.SetRoomFeedbackController(room2FeedbackController);
        }
        
        if (room2RedStone != null)
        {
            room2RedStone.SetStoneType(FeedbackStoneInteraction.StoneType.Red);
            room2RedStone.SetAssignedPlayer(2);
            if (room2FeedbackController != null)
                room2RedStone.SetRoomFeedbackController(room2FeedbackController);
        }
        
        if (room2PictureFrame != null)
        {
            room2PictureFrame.SetAssignedPlayer(2);
        }
        
        if (room2FeedbackController != null)
        {
            room2FeedbackController.SetAssignedPlayer(2);
        }
    }
    
    private void EstablishConnections()
    {
        if (showSetupProgress) Debug.Log("Establishing connections...");
        
        // Register displays with message relay
        if (messageRelay != null)
        {
            if (room1TextDisplay != null)
                messageRelay.RegisterMagicBookDisplay(room1TextDisplay, 1);
        }
        
        // Set voice recognition manager reference
        if (voiceHandler != null)
        {
            VoiceRecognitionManager vrManager = FindFirstObjectByType<VoiceRecognitionManager>();
            if (vrManager != null)
                voiceHandler.SetVoiceRecognitionManager(vrManager);
        }
        
        // Set observer for picture frame
        if (room2PictureFrame != null)
        {
            Camera playerCamera = Camera.main;
            if (playerCamera != null)
                room2PictureFrame.SetObserverTransform(playerCamera.transform);
        }
    }
    
    private bool ValidateSetup()
    {
        if (showSetupProgress) Debug.Log("Validating setup...");
        
        bool isValid = true;
        
        // Check core components
        if (gameManager == null)
        {
            Debug.LogError("WordGuessingGameManager is missing!");
            isValid = false;
        }
        
        if (roleManager == null)
        {
            Debug.LogError("PlayerRoleManager is missing!");
            isValid = false;
        }
        
        if (voiceHandler == null)
        {
            Debug.LogWarning("WordGuessingVoiceHandler is missing - voice features will be disabled");
        }
        
        // Check room components
        if (room1MagicBook == null)
        {
            Debug.LogWarning("Room 1 Magic Book is missing");
        }
        
        if (room2PictureFrame == null)
        {
            Debug.LogWarning("Room 2 Picture Frame is missing");
        }
        
        if (room2BlueStone == null || room2RedStone == null)
        {
            Debug.LogWarning("Room 2 Feedback Stones are missing");
        }
        
        return isValid;
    }
    
    private void InitializeGame()
    {
        if (showSetupProgress) Debug.Log("Initializing game...");
        
        // Connect players (simulated for testing)
        if (roleManager != null)
        {
            roleManager.ConnectPlayer(1, "Player 1");
            roleManager.ConnectPlayer(2, "Player 2");
        }
        
        // Set initial difficulty
        if (gameManager != null)
        {
            var wordDatabase = gameManager.GetComponent<WordDatabase>();
            if (wordDatabase != null)
            {
                wordDatabase.SetDifficulty(config.startingDifficulty);
            }
        }
        
        if (debugMode)
        {
            Debug.Log("Game ready to start! Call WordGuessingGameManager.Instance.StartGame() to begin.");
        }
    }
    
    [ContextMenu("Start Game")]
    public void StartGame()
    {
        if (!isSetupComplete)
        {
            SetupGame();
        }
        
        if (gameManager != null)
        {
            gameManager.StartGame();
        }
        else
        {
            Debug.LogError("Cannot start game - GameManager is missing!");
        }
    }
    
    [ContextMenu("Reset Game")]
    public void ResetGame()
    {
        if (gameManager != null)
        {
            gameManager.EndGame();
        }
        
        if (roleManager != null)
        {
            roleManager.ResetAllPlayers();
        }
        
        if (messageRelay != null)
        {
            messageRelay.ClearMessageHistory();
        }
        
        Debug.Log("Game reset complete");
    }
    
    public void UpdateConfiguration(GameSetupConfiguration newConfig)
    {
        config = newConfig;
        ConfigureComponents();
        
        if (debugMode)
        {
            Debug.Log("Configuration updated");
        }
    }
    
    public GameSetupConfiguration GetConfiguration()
    {
        return config;
    }
    
    public bool IsSetupComplete()
    {
        return isSetupComplete;
    }
    
    // Utility methods for external integration
    public void SetPlayerPosition(int playerId, Vector3 position)
    {
        if (roleManager != null)
        {
            var playerData = roleManager.GetPlayerData(playerId);
            if (playerData != null)
            {
                playerData.roomPosition = position;
            }
        }
    }
    
    public void SetVoiceRecognitionManager(VoiceRecognitionManager vrManager)
    {
        if (voiceHandler != null)
        {
            voiceHandler.SetVoiceRecognitionManager(vrManager);
        }
    }
    
    private void OnValidate()
    {
        // Ensure configuration values are valid
        config.wordsPerStage = Mathf.Max(1, config.wordsPerStage);
        config.roundTimeLimit = Mathf.Max(30f, config.roundTimeLimit);
        config.voiceConfidenceThreshold = Mathf.Clamp01(config.voiceConfidenceThreshold);
        config.voiceTimeout = Mathf.Max(1f, config.voiceTimeout);
        config.uiTransitionSpeed = Mathf.Max(0.1f, config.uiTransitionSpeed);
    }
}