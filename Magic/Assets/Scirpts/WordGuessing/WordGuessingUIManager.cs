using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class WordGuessingUIManager : MonoBehaviour
{
    public static WordGuessingUIManager Instance { get; private set; }
    
    [Header("Main UI Panels")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private GameObject gameHUD;
    [SerializeField] private GameObject successPanel;
    [SerializeField] private GameObject failurePanel;
    [SerializeField] private GameObject stageCompletePanel;
    [SerializeField] private GameObject settingsPanel;
    
    [Header("Game Information")]
    [SerializeField] private TextMeshProUGUI currentRoleText;
    [SerializeField] private TextMeshProUGUI currentPhaseText;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI roundText;
    [SerializeField] private TextMeshProUGUI instructionText;
    
    [Header("Progress Indicators")]
    [SerializeField] private Slider stageProgressSlider;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image[] playerIndicators;
    [SerializeField] private Color activePlayerColor = Color.green;
    [SerializeField] private Color inactivePlayerColor = Color.gray;
    
    [Header("Notification System")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image notificationBackground;
    [SerializeField] private float notificationDuration = 3f;
    [SerializeField] private AnimationCurve notificationFadeCurve;
    
    [Header("Success/Failure Feedback")]
    [SerializeField] private TextMeshProUGUI successMessageText;
    [SerializeField] private TextMeshProUGUI failureMessageText;
    [SerializeField] private TextMeshProUGUI stageCompleteMessageText;
    [SerializeField] private Button continueButton;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button nextStageButton;
    
    [Header("Settings UI")]
    [SerializeField] private Slider difficultySlider;
    [SerializeField] private Toggle showHintsToggle;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Button closeSettingsButton;
    
    [Header("Debug UI")]
    [SerializeField] private GameObject debugPanel;
    [SerializeField] private TextMeshProUGUI debugText;
    [SerializeField] private Button debugToggleButton;
    [SerializeField] private bool showDebugUI = false;
    
    [Header("Animation Settings")]
    [SerializeField] private float panelTransitionDuration = 0.5f;
    [SerializeField] private AnimationCurve panelTransitionCurve;
    [SerializeField] private bool useScaleAnimation = true;
    [SerializeField] private bool useFadeAnimation = true;
    
    private Coroutine notificationCoroutine;
    private Coroutine panelTransitionCoroutine;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeUI();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private void OnEnable()
    {
        SubscribeToEvents();
    }
    
    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }
    
    private void InitializeUI()
    {
        // Hide all panels initially
        HideAllPanels();
        
        // Show main HUD
        if (gameHUD != null)
            gameHUD.SetActive(true);
        
        // Setup buttons
        SetupButtons();
        
        // Initialize debug UI
        if (debugPanel != null)
            debugPanel.SetActive(showDebugUI);
        
        Debug.Log("WordGuessingUIManager initialized");
    }
    
    private void SubscribeToEvents()
    {
        WordGuessingGameManager.OnGamePhaseChanged += HandleGamePhaseChanged;
        WordGuessingGameManager.OnScoreUpdated += HandleScoreUpdated;
        WordGuessingGameManager.OnStageComplete += HandleStageComplete;
        WordGuessingGameManager.OnGameMessage += HandleGameMessage;
        PlayerRoleManager.OnPlayerRoleChanged += HandlePlayerRoleChanged;
        PlayerRoleManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
        WordGuessingVoiceHandler.OnVoiceActivated += HandleVoiceActivated;
        WordGuessingVoiceHandler.OnVoiceDeactivated += HandleVoiceDeactivated;
    }
    
    private void UnsubscribeFromEvents()
    {
        WordGuessingGameManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
        WordGuessingGameManager.OnScoreUpdated -= HandleScoreUpdated;
        WordGuessingGameManager.OnStageComplete -= HandleStageComplete;
        WordGuessingGameManager.OnGameMessage -= HandleGameMessage;
        PlayerRoleManager.OnPlayerRoleChanged -= HandlePlayerRoleChanged;
        PlayerRoleManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
        WordGuessingVoiceHandler.OnVoiceActivated -= HandleVoiceActivated;
        WordGuessingVoiceHandler.OnVoiceDeactivated -= HandleVoiceDeactivated;
    }
    
    private void SetupButtons()
    {
        if (continueButton != null)
            continueButton.onClick.AddListener(OnContinueButtonClicked);
        
        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryButtonClicked);
        
        if (nextStageButton != null)
            nextStageButton.onClick.AddListener(OnNextStageButtonClicked);
        
        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(OnCloseSettingsButtonClicked);
        
        if (debugToggleButton != null)
            debugToggleButton.onClick.AddListener(OnDebugToggleButtonClicked);
    }
    
    private void HandleGamePhaseChanged(WordGuessingGameManager.GamePhase newPhase)
    {
        UpdatePhaseText(newPhase);
        UpdateInstructionText(newPhase);
        
        switch (newPhase)
        {
            case WordGuessingGameManager.GamePhase.RoundStart:
                HideAllPanels();
                ShowGameHUD();
                break;
                
            case WordGuessingGameManager.GamePhase.QuestionPhase:
                ShowNotification("Ask questions to guess the word!");
                break;
                
            case WordGuessingGameManager.GamePhase.FeedbackPhase:
                ShowNotification("Waiting for feedback...");
                break;
                
            case WordGuessingGameManager.GamePhase.GuessPhase:
                ShowNotification("Make your guess now!");
                break;
                
            case WordGuessingGameManager.GamePhase.RoundEnd:
                // Success/failure will be handled by other events
                break;
                
            case WordGuessingGameManager.GamePhase.StageComplete:
                ShowStageCompletePanel();
                break;
        }
    }
    
    private void HandleScoreUpdated(int player1Score, int player2Score)
    {
        UpdateScoreDisplay(player1Score, player2Score);
        UpdateProgressBar();
    }
    
    private void HandleStageComplete()
    {
        ShowStageCompletePanel();
        ShowNotification("Stage Complete! Well done!");
    }
    
    private void HandleGameMessage(string message)
    {
        ShowNotification(message);
        UpdateDebugText($"Game Message: {message}");
    }
    
    private void HandlePlayerRoleChanged(int playerId, WordGuessingGameManager.PlayerRole role)
    {
        UpdateRoleDisplay();
        UpdatePlayerIndicators();
        
        string roleMessage = $"You are now the {role}";
        ShowNotification(roleMessage);
    }
    
    private void HandlePlayerDataUpdated(PlayerRoleManager.PlayerData playerData)
    {
        UpdatePlayerIndicators();
        UpdateDebugText($"Player {playerData.playerId} updated: {playerData.currentRole}");
    }
    
    private void HandleVoiceActivated(int playerId)
    {
        ShowNotification("Voice recognition active - speak now!");
        UpdateDebugText($"Voice activated for Player {playerId}");
    }
    
    private void HandleVoiceDeactivated(int playerId)
    {
        ShowNotification("Voice recognition stopped");
        UpdateDebugText($"Voice deactivated for Player {playerId}");
    }
    
    private void UpdatePhaseText(WordGuessingGameManager.GamePhase phase)
    {
        if (currentPhaseText != null)
        {
            string phaseString = phase.ToString().Replace("Phase", "");
            currentPhaseText.text = $"Phase: {phaseString}";
        }
    }
    
    private void UpdateRoleDisplay()
    {
        if (currentRoleText != null && PlayerRoleManager.Instance != null)
        {
            // Assuming Player 1 for now - this could be made dynamic
            var role = PlayerRoleManager.Instance.GetPlayerRole(1);
            currentRoleText.text = $"Role: {role}";
        }
    }
    
    private void UpdateInstructionText(WordGuessingGameManager.GamePhase phase)
    {
        if (instructionText == null) return;
        
        string instruction = GetInstructionForPhase(phase);
        instructionText.text = instruction;
    }
    
    private string GetInstructionForPhase(WordGuessingGameManager.GamePhase phase)
    {
        if (PlayerRoleManager.Instance == null) return "";
        
        var role = PlayerRoleManager.Instance.GetPlayerRole(1); // Assuming Player 1
        
        switch (phase)
        {
            case WordGuessingGameManager.GamePhase.RoundStart:
                return "Get ready for the next round!";
                
            case WordGuessingGameManager.GamePhase.QuestionPhase:
                if (role == WordGuessingGameManager.PlayerRole.Guesser)
                    return "Place your hand on the magic book and ask questions!";
                else
                    return "Listen to questions and prepare to give feedback!";
                
            case WordGuessingGameManager.GamePhase.FeedbackPhase:
                if (role == WordGuessingGameManager.PlayerRole.Describer)
                    return "Use blue stone for YES, red stone for NO!";
                else
                    return "Wait for feedback from the describer!";
                
            case WordGuessingGameManager.GamePhase.GuessPhase:
                if (role == WordGuessingGameManager.PlayerRole.Guesser)
                    return "Make your final guess using the magic book!";
                else
                    return "Listen for the final guess!";
                
            default:
                return "";
        }
    }
    
    private void UpdateScoreDisplay(int player1Score, int player2Score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {player1Score} - {player2Score}";
        }
        
        if (roundText != null && WordGuessingGameManager.Instance != null)
        {
            var gameState = WordGuessingGameManager.Instance.GetGameState();
            roundText.text = $"Round: {gameState.currentRound}";
        }
    }
    
    private void UpdateProgressBar()
    {
        if (stageProgressSlider != null && WordGuessingGameManager.Instance != null)
        {
            var gameState = WordGuessingGameManager.Instance.GetGameState();
            float progress = (float)gameState.wordsGuessedThisStage / gameState.requiredWordsPerStage;
            stageProgressSlider.value = progress;
        }
        
        if (progressText != null && WordGuessingGameManager.Instance != null)
        {
            var gameState = WordGuessingGameManager.Instance.GetGameState();
            progressText.text = $"{gameState.wordsGuessedThisStage}/{gameState.requiredWordsPerStage}";
        }
    }
    
    private void UpdatePlayerIndicators()
    {
        if (playerIndicators == null || PlayerRoleManager.Instance == null) return;
        
        for (int i = 0; i < playerIndicators.Length; i++)
        {
            if (playerIndicators[i] != null)
            {
                int playerId = i + 1;
                bool isConnected = PlayerRoleManager.Instance.IsPlayerConnected(playerId);
                playerIndicators[i].color = isConnected ? activePlayerColor : inactivePlayerColor;
            }
        }
    }
    
    public void ShowNotification(string message)
    {
        if (notificationPanel == null || notificationText == null) return;
        
        if (notificationCoroutine != null)
            StopCoroutine(notificationCoroutine);
        
        notificationCoroutine = StartCoroutine(ShowNotificationCoroutine(message));
    }
    
    private IEnumerator ShowNotificationCoroutine(string message)
    {
        notificationText.text = message;
        notificationPanel.SetActive(true);
        
        // Fade in
        if (useFadeAnimation)
        {
            yield return StartCoroutine(FadeNotification(0f, 1f, 0.3f));
        }
        
        // Hold
        yield return new WaitForSeconds(notificationDuration);
        
        // Fade out
        if (useFadeAnimation)
        {
            yield return StartCoroutine(FadeNotification(1f, 0f, 0.3f));
        }
        
        notificationPanel.SetActive(false);
        notificationCoroutine = null;
    }
    
    private IEnumerator FadeNotification(float startAlpha, float endAlpha, float duration)
    {
        if (notificationBackground == null) yield break;
        
        float elapsedTime = 0f;
        Color startColor = notificationBackground.color;
        Color endColor = startColor;
        startColor.a = startAlpha;
        endColor.a = endAlpha;
        
        while (elapsedTime < duration)
        {
            float progress = elapsedTime / duration;
            float curveValue = notificationFadeCurve?.Evaluate(progress) ?? progress;
            
            notificationBackground.color = Color.Lerp(startColor, endColor, curveValue);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        notificationBackground.color = endColor;
    }
    
    private void ShowGameHUD()
    {
        if (gameHUD != null)
            gameHUD.SetActive(true);
    }
    
    private void ShowStageCompletePanel()
    {
        HideAllPanels();
        
        if (stageCompletePanel != null)
        {
            if (stageCompleteMessageText != null)
            {
                stageCompleteMessageText.text = "Congratulations!\nStage Complete!";
            }
            
            ShowPanelWithAnimation(stageCompletePanel);
        }
    }
    
    private void ShowPanelWithAnimation(GameObject panel)
    {
        if (panel == null) return;
        
        if (panelTransitionCoroutine != null)
            StopCoroutine(panelTransitionCoroutine);
        
        panelTransitionCoroutine = StartCoroutine(ShowPanelAnimation(panel));
    }
    
    private IEnumerator ShowPanelAnimation(GameObject panel)
    {
        panel.SetActive(true);
        
        if (useScaleAnimation)
        {
            Transform panelTransform = panel.transform;
            Vector3 originalScale = panelTransform.localScale;
            panelTransform.localScale = Vector3.zero;
            
            float elapsedTime = 0f;
            while (elapsedTime < panelTransitionDuration)
            {
                float progress = elapsedTime / panelTransitionDuration;
                float curveValue = panelTransitionCurve?.Evaluate(progress) ?? progress;
                
                panelTransform.localScale = Vector3.Lerp(Vector3.zero, originalScale, curveValue);
                
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            
            panelTransform.localScale = originalScale;
        }
        
        panelTransitionCoroutine = null;
    }
    
    private void HideAllPanels()
    {
        if (successPanel != null) successPanel.SetActive(false);
        if (failurePanel != null) failurePanel.SetActive(false);
        if (stageCompletePanel != null) stageCompletePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }
    
    private void UpdateDebugText(string message)
    {
        if (debugText != null && showDebugUI)
        {
            string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
            debugText.text += $"[{timestamp}] {message}\n";
            
            // Limit debug text length
            string[] lines = debugText.text.Split('\n');
            if (lines.Length > 20)
            {
                debugText.text = string.Join("\n", lines, lines.Length - 20, 20);
            }
        }
    }
    
    // Button event handlers
    private void OnContinueButtonClicked()
    {
        HideAllPanels();
        ShowGameHUD();
    }
    
    private void OnRetryButtonClicked()
    {
        if (WordGuessingGameManager.Instance != null)
        {
            WordGuessingGameManager.Instance.StartGame();
        }
        HideAllPanels();
        ShowGameHUD();
    }
    
    private void OnNextStageButtonClicked()
    {
        // This would advance to the next stage in your game progression
        HideAllPanels();
        ShowGameHUD();
        ShowNotification("Starting next stage...");
    }
    
    private void OnCloseSettingsButtonClicked()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
        ShowGameHUD();
    }
    
    private void OnDebugToggleButtonClicked()
    {
        showDebugUI = !showDebugUI;
        if (debugPanel != null)
            debugPanel.SetActive(showDebugUI);
    }
    
    public void ShowSettingsPanel()
    {
        HideAllPanels();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }
    
    public void SetUIEnabled(bool enabled)
    {
        if (mainCanvas != null)
            mainCanvas.enabled = enabled;
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}