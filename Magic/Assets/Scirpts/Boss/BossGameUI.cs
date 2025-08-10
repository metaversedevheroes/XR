using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class BossGameUI : MonoBehaviour
{
    [System.Serializable]
    public class UIElements
    {
        [Header("Health Bars")]
        public Slider playerHealthBar;
        public Slider bossHealthBar;
        public TextMeshProUGUI playerHealthText;
        public TextMeshProUGUI bossHealthText;
        
        [Header("Sentence Display")]
        public TextMeshProUGUI sentenceText;
        public TextMeshProUGUI hintText;
        public GameObject sentencePanel;
        
        [Header("Vocabulary Challenge")]
        public TextMeshProUGUI vocabularyText;
        public TextMeshProUGUI vocabularyProgressText;
        public GameObject vocabularyPanel;
        
        [Header("Game Messages")]
        public TextMeshProUGUI gameMessageText;
        public GameObject messagePanel;
        
        [Header("Timer")]
        public Slider timerBar;
        public TextMeshProUGUI timerText;
        
        [Header("Combat Status")]
        public TextMeshProUGUI comboText;
        public TextMeshProUGUI phaseText;
        public GameObject listeningIndicator;
        
        [Header("Boss Room")]
        public GameObject bossRoomUI;
        public Button startBattleButton;
        public Button exitRoomButton;
    }
    
    [Header("UI Configuration")]
    [SerializeField] private UIElements ui = new UIElements();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float messageDisplayDuration = 3f;
    [SerializeField] private float sentenceDisplayDuration = 15f;
    
    [Header("Animation Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.3f;
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Color Settings")]
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color injuredColor = Color.yellow;
    [SerializeField] private Color criticalColor = Color.red;
    [SerializeField] private Color correctAnswerColor = Color.green;
    [SerializeField] private Color incorrectAnswerColor = Color.red;
    [SerializeField] private Color hintColor = Color.blue;
    
    private BossGameManager gameManager;
    private SpeechCombatSystem speechSystem;
    private PlayerHealthSystem playerHealth;
    private Coroutine messageCoroutine;
    private Coroutine timerCoroutine;
    private bool isUIActive = false;
    
    private void Awake()
    {
        FindSystemReferences();
        InitializeUI();
    }
    
    private void Start()
    {
        SubscribeToEvents();
        SetupInitialUI();
    }
    
    private void FindSystemReferences()
    {
        gameManager = FindFirstObjectByType<BossGameManager>();
        speechSystem = FindFirstObjectByType<SpeechCombatSystem>();
        playerHealth = FindFirstObjectByType<PlayerHealthSystem>();
        
        if (debugMode)
        {
            Debug.Log($"UI References - GameManager: {gameManager != null}, SpeechSystem: {speechSystem != null}, PlayerHealth: {playerHealth != null}");
        }
    }
    
    private void InitializeUI()
    {
        // Initialize UI elements to default states
        if (ui.sentencePanel != null) ui.sentencePanel.SetActive(false);
        if (ui.vocabularyPanel != null) ui.vocabularyPanel.SetActive(false);
        if (ui.messagePanel != null) ui.messagePanel.SetActive(false);
        if (ui.listeningIndicator != null) ui.listeningIndicator.SetActive(false);
        
        // Setup button events
        if (ui.startBattleButton != null)
        {
            ui.startBattleButton.onClick.AddListener(OnStartBattleClicked);
        }
        
        if (ui.exitRoomButton != null)
        {
            ui.exitRoomButton.onClick.AddListener(OnExitRoomClicked);
        }
        
        // Initialize health bars
        if (ui.playerHealthBar != null)
        {
            ui.playerHealthBar.value = 1f;
            ui.playerHealthBar.fillRect.GetComponent<Image>().color = healthyColor;
        }
        
        if (ui.bossHealthBar != null)
        {
            ui.bossHealthBar.value = 1f;
        }
        
        if (debugMode) Debug.Log("Boss Game UI initialized");
    }
    
    private void SubscribeToEvents()
    {
        // Game Manager Events
        if (gameManager != null)
        {
            BossGameManager.OnGamePhaseChanged += HandleGamePhaseChanged;
            BossGameManager.OnPlayerHealthChanged += HandlePlayerHealthChanged;
            BossGameManager.OnBossHealthChanged += HandleBossHealthChanged;
            BossGameManager.OnGameMessage += HandleGameMessage;
        }
        
        // Speech System Events
        if (speechSystem != null)
        {
            SpeechCombatSystem.OnNewSentencePresented += HandleNewSentence;
            SpeechCombatSystem.OnVocabularySequenceStarted += HandleVocabularySequence;
            SpeechCombatSystem.OnFeedbackMessage += HandleFeedbackMessage;
            SpeechCombatSystem.OnSpeechRecognized += HandleSpeechRecognized;
        }
        
        // Player Health Events
        if (playerHealth != null)
        {
            PlayerHealthSystem.OnHealthStateChanged += HandlePlayerHealthStateChanged;
        }
    }
    
    private void SetupInitialUI()
    {
        if (ui.bossRoomUI != null)
        {
            ui.bossRoomUI.SetActive(true);
        }
        
        UpdatePhaseDisplay("Waiting to Start");
        UpdateComboDisplay(0);
        
        isUIActive = true;
    }
    
    private void HandleGamePhaseChanged(BossGameManager.GamePhase phase)
    {
        UpdatePhaseDisplay(phase.ToString());
        
        switch (phase)
        {
            case BossGameManager.GamePhase.BossIntroduction:
                ShowBossIntroUI();
                break;
            case BossGameManager.GamePhase.Combat:
                ShowCombatUI();
                break;
            case BossGameManager.GamePhase.BossDefeated:
                ShowVictoryUI();
                break;
            case BossGameManager.GamePhase.GameOver:
                ShowGameOverUI();
                break;
        }
        
        if (debugMode) Debug.Log($"UI updated for phase: {phase}");
    }
    
    private void HandlePlayerHealthChanged(int currentHealth, int maxHealth)
    {
        float healthPercentage = (float)currentHealth / maxHealth;
        
        if (ui.playerHealthBar != null)
        {
            ui.playerHealthBar.value = healthPercentage;
            UpdateHealthBarColor(ui.playerHealthBar, healthPercentage);
        }
        
        if (ui.playerHealthText != null)
        {
            ui.playerHealthText.text = $"{currentHealth}/{maxHealth}";
        }
        
        // Add screen effects for low health
        if (healthPercentage <= 0.25f)
        {
            StartCoroutine(CriticalHealthEffect());
        }
    }
    
    private void HandleBossHealthChanged(int currentHealth, int maxHealth)
    {
        float healthPercentage = (float)currentHealth / maxHealth;
        
        if (ui.bossHealthBar != null)
        {
            ui.bossHealthBar.value = healthPercentage;
        }
        
        if (ui.bossHealthText != null)
        {
            ui.bossHealthText.text = $"{currentHealth}/{maxHealth}";
        }
    }
    
    private void HandleGameMessage(string message)
    {
        ShowMessage(message, messageDisplayDuration);
    }
    
    private void HandleNewSentence(EnglishSentence sentence)
    {
        if (ui.sentencePanel != null)
        {
            ui.sentencePanel.SetActive(true);
        }
        
        if (ui.vocabularyPanel != null)
        {
            ui.vocabularyPanel.SetActive(false);
        }
        
        if (ui.sentenceText != null)
        {
            ui.sentenceText.text = sentence.sentenceWithBlanks;
            ui.sentenceText.color = Color.white;
        }
        
        if (ui.hintText != null && !string.IsNullOrEmpty(sentence.hint))
        {
            ui.hintText.text = $"Hint: {sentence.hint}";
            ui.hintText.color = hintColor;
        }
        
        StartSentenceTimer();
        ShowListeningIndicator(true);
        
        if (debugMode) Debug.Log($"Displayed sentence: {sentence.sentenceWithBlanks}");
    }
    
    private void HandleVocabularySequence(List<string> sequence, int currentIndex)
    {
        if (ui.vocabularyPanel != null)
        {
            ui.vocabularyPanel.SetActive(true);
        }
        
        if (ui.sentencePanel != null)
        {
            ui.sentencePanel.SetActive(false);
        }
        
        if (ui.vocabularyText != null && sequence.Count > currentIndex)
        {
            ui.vocabularyText.text = $"Say: {sequence[currentIndex]}";
        }
        
        if (ui.vocabularyProgressText != null)
        {
            ui.vocabularyProgressText.text = $"Progress: {currentIndex + 1}/{sequence.Count}";
        }
        
        ShowListeningIndicator(true);
        
        if (debugMode) Debug.Log($"Vocabulary challenge: {currentIndex + 1}/{sequence.Count}");
    }
    
    private void HandleFeedbackMessage(string feedback)
    {
        bool isCorrect = feedback.Contains("Correct") || feedback.Contains("Perfect");
        Color feedbackColor = isCorrect ? correctAnswerColor : incorrectAnswerColor;
        
        ShowMessage(feedback, 2f, feedbackColor);
        
        if (isCorrect)
        {
            StartCoroutine(CorrectAnswerEffect());
        }
    }
    
    private void HandleSpeechRecognized(string recognizedText)
    {
        ShowMessage($"Heard: {recognizedText}", 1f, Color.cyan);
        ShowListeningIndicator(false);
    }
    
    private void HandlePlayerHealthStateChanged(PlayerHealthSystem.HealthState state)
    {
        switch (state)
        {
            case PlayerHealthSystem.HealthState.Critical:
                StartCoroutine(CriticalHealthWarning());
                break;
            case PlayerHealthSystem.HealthState.Dead:
                ShowGameOverUI();
                break;
        }
    }
    
    private void ShowMessage(string message, float duration, Color? color = null)
    {
        if (ui.gameMessageText == null) return;
        
        if (messageCoroutine != null)
        {
            StopCoroutine(messageCoroutine);
        }
        
        messageCoroutine = StartCoroutine(DisplayMessageCoroutine(message, duration, color ?? Color.white));
    }
    
    private IEnumerator DisplayMessageCoroutine(string message, float duration, Color color)
    {
        if (ui.messagePanel != null)
        {
            ui.messagePanel.SetActive(true);
        }
        
        ui.gameMessageText.text = message;
        ui.gameMessageText.color = color;
        
        // Fade in
        yield return StartCoroutine(FadeText(ui.gameMessageText, 0f, 1f, fadeInDuration));
        
        // Display
        yield return new WaitForSeconds(duration);
        
        // Fade out
        yield return StartCoroutine(FadeText(ui.gameMessageText, 1f, 0f, fadeOutDuration));
        
        if (ui.messagePanel != null)
        {
            ui.messagePanel.SetActive(false);
        }
    }
    
    private void StartSentenceTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
        
        timerCoroutine = StartCoroutine(SentenceTimerCoroutine());
    }
    
    private IEnumerator SentenceTimerCoroutine()
    {
        float timeRemaining = sentenceDisplayDuration;
        
        while (timeRemaining > 0)
        {
            if (ui.timerBar != null)
            {
                ui.timerBar.value = timeRemaining / sentenceDisplayDuration;
            }
            
            if (ui.timerText != null)
            {
                ui.timerText.text = $"Time: {Mathf.Ceil(timeRemaining)}s";
            }
            
            timeRemaining -= Time.deltaTime;
            yield return null;
        }
        
        // Time expired
        if (ui.timerBar != null)
        {
            ui.timerBar.value = 0f;
        }
        
        ShowListeningIndicator(false);
    }
    
    private void ShowListeningIndicator(bool show)
    {
        if (ui.listeningIndicator != null)
        {
            ui.listeningIndicator.SetActive(show);
        }
    }
    
    private void UpdateHealthBarColor(Slider healthBar, float healthPercentage)
    {
        if (healthBar.fillRect == null) return;
        
        Image fillImage = healthBar.fillRect.GetComponent<Image>();
        if (fillImage == null) return;
        
        if (healthPercentage > 0.5f)
        {
            fillImage.color = healthyColor;
        }
        else if (healthPercentage > 0.25f)
        {
            fillImage.color = injuredColor;
        }
        else
        {
            fillImage.color = criticalColor;
        }
    }
    
    private void UpdatePhaseDisplay(string phase)
    {
        if (ui.phaseText != null)
        {
            ui.phaseText.text = $"Phase: {phase}";
        }
    }
    
    private void UpdateComboDisplay(int combo)
    {
        if (ui.comboText != null)
        {
            if (combo > 0)
            {
                ui.comboText.text = $"Combo: x{combo}";
                ui.comboText.color = correctAnswerColor;
            }
            else
            {
                ui.comboText.text = "Combo: x0";
                ui.comboText.color = Color.white;
            }
        }
    }
    
    private void ShowBossIntroUI()
    {
        ShowMessage("A mighty dragon appears!", 3f, Color.red);
    }
    
    private void ShowCombatUI()
    {
        ShowMessage("Battle begins! Speak correctly to attack!", 2f, Color.yellow);
    }
    
    private void ShowVictoryUI()
    {
        ShowMessage("Victory! The dragon has been defeated!", 5f, Color.gold);
        StartCoroutine(VictoryEffect());
    }
    
    private void ShowGameOverUI()
    {
        ShowMessage("Game Over! Restarting battle...", 3f, Color.red);
    }
    
    private IEnumerator CorrectAnswerEffect()
    {
        // Flash effect for correct answers
        if (ui.sentenceText != null)
        {
            Color originalColor = ui.sentenceText.color;
            ui.sentenceText.color = correctAnswerColor;
            
            yield return new WaitForSeconds(0.3f);
            
            ui.sentenceText.color = originalColor;
        }
    }
    
    private IEnumerator CriticalHealthEffect()
    {
        // Screen pulsing effect for critical health
        float duration = 1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            float alpha = Mathf.Sin(elapsedTime * 10f) * 0.3f + 0.3f;
            // Apply red overlay effect here
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator CriticalHealthWarning()
    {
        ShowMessage("CRITICAL HEALTH!", 2f, criticalColor);
        yield return null;
    }
    
    private IEnumerator VictoryEffect()
    {
        // Victory celebration effect
        float duration = 3f;
        float elapsedTime = 0f;
        
        while (elapsedTime < duration)
        {
            // Add sparkle or celebration effects
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator FadeText(TextMeshProUGUI text, float startAlpha, float endAlpha, float duration)
    {
        float elapsedTime = 0f;
        Color originalColor = text.color;
        
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            float alpha = Mathf.Lerp(startAlpha, endAlpha, fadeCurve.Evaluate(t));
            
            text.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, endAlpha);
    }
    
    private void OnStartBattleClicked()
    {
        if (gameManager != null)
        {
            gameManager.PlayerEnteredBossRoom();
        }
        
        if (ui.startBattleButton != null)
        {
            ui.startBattleButton.gameObject.SetActive(false);
        }
    }
    
    private void OnExitRoomClicked()
    {
        if (gameManager != null)
        {
            gameManager.PlayerExitedBossRoom();
        }
    }
    
    public void SetUIActive(bool active)
    {
        isUIActive = active;
        
        if (ui.bossRoomUI != null)
        {
            ui.bossRoomUI.SetActive(active);
        }
    }
    
    public void ShowStartButton(bool show)
    {
        if (ui.startBattleButton != null)
        {
            ui.startBattleButton.gameObject.SetActive(show);
        }
    }
    
    private void OnDestroy()
    {
        // Unsubscribe from events
        if (gameManager != null)
        {
            BossGameManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
            BossGameManager.OnPlayerHealthChanged -= HandlePlayerHealthChanged;
            BossGameManager.OnBossHealthChanged -= HandleBossHealthChanged;
            BossGameManager.OnGameMessage -= HandleGameMessage;
        }
        
        if (speechSystem != null)
        {
            SpeechCombatSystem.OnNewSentencePresented -= HandleNewSentence;
            SpeechCombatSystem.OnVocabularySequenceStarted -= HandleVocabularySequence;
            SpeechCombatSystem.OnFeedbackMessage -= HandleFeedbackMessage;
            SpeechCombatSystem.OnSpeechRecognized -= HandleSpeechRecognized;
        }
        
        if (playerHealth != null)
        {
            PlayerHealthSystem.OnHealthStateChanged -= HandlePlayerHealthStateChanged;
        }
    }
}