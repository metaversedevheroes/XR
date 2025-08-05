using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class PictureFrameDisplay : MonoBehaviour
{
    [Header("Display Components")]
    [SerializeField] private Image wordDisplayImage;
    [SerializeField] private TextMeshProUGUI wordDisplayText;
    [SerializeField] private Canvas displayCanvas;
    [SerializeField] private RectTransform frameTransform;
    
    [Header("Visual Settings")]
    [SerializeField] private TMP_FontAsset displayFont;
    [SerializeField] private Color textColor = Color.black;
    [SerializeField] private Color backgroundColor = Color.white;
    [SerializeField] private int fontSize = 48;
    [SerializeField] private TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
    
    [Header("Frame Configuration")]
    [SerializeField] private int assignedPlayerId = 2; // Default to describer
    [SerializeField] private bool onlyVisibleToDescriber = true;
    [SerializeField] private LayerMask visibilityLayer = 1;
    
    [Header("Animation")]
    [SerializeField] private bool useWordRevealAnimation = true;
    [SerializeField] private float revealDuration = 1f;
    [SerializeField] private AnimationCurve revealCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool useShimmerEffect = true;
    [SerializeField] private float shimmerSpeed = 2f;
    
    [Header("Word Hints")]
    [SerializeField] private bool showWordHints = false;
    [SerializeField] private TextMeshProUGUI hintText;
    [SerializeField] private Color hintTextColor = Color.gray;
    [SerializeField] private int hintFontSize = 24;
    
    [Header("Frame Effects")]
    [SerializeField] private ParticleSystem frameParticles;
    [SerializeField] private Light frameLight;
    [SerializeField] private AudioSource frameAudioSource;
    [SerializeField] private AudioClip wordAppearSound;
    [SerializeField] private AudioClip wordChangeSound;
    
    [Header("Security & Privacy")]
    [SerializeField] private bool usePrivacyShader = true;
    [SerializeField] private Material privacyMaterial;
    [SerializeField] private float viewingAngle = 60f;
    [SerializeField] private Transform observerTransform;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showDebugInfo = false;
    [SerializeField] private string currentDisplayWord = "";
    
    private string currentWord = "";
    private bool isWordVisible = false;
    private bool canCurrentPlayerSee = false;
    private Coroutine revealCoroutine;
    private Coroutine shimmerCoroutine;
    private Material originalMaterial;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        SetupDisplay();
        UpdateVisibility();
    }
    
    private void OnEnable()
    {
        WordGuessingGameManager.OnNewWordSelected += HandleNewWordSelected;
        WordGuessingGameManager.OnGamePhaseChanged += HandleGamePhaseChanged;
        PlayerRoleManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
        PlayerRoleManager.OnPlayerPermissionsChanged += HandlePlayerPermissionsChanged;
    }
    
    private void OnDisable()
    {
        WordGuessingGameManager.OnNewWordSelected -= HandleNewWordSelected;
        WordGuessingGameManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
        PlayerRoleManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
        PlayerRoleManager.OnPlayerPermissionsChanged -= HandlePlayerPermissionsChanged;
    }
    
    private void InitializeComponents()
    {
        if (displayCanvas == null)
            displayCanvas = GetComponentInChildren<Canvas>();
        
        if (wordDisplayText == null)
            wordDisplayText = GetComponentInChildren<TextMeshProUGUI>();
        
        if (wordDisplayImage == null)
            wordDisplayImage = GetComponentInChildren<Image>();
        
        if (frameTransform == null)
            frameTransform = GetComponent<RectTransform>();
        
        if (frameAudioSource == null)
            frameAudioSource = GetComponent<AudioSource>();
        
        if (frameLight == null)
            frameLight = GetComponentInChildren<Light>();
        
        if (observerTransform == null)
        {
            // Try to find the player camera or assigned player
            Camera playerCamera = Camera.main;
            if (playerCamera != null)
                observerTransform = playerCamera.transform;
        }
        
        if (debugMode)
        {
            Debug.Log($"PictureFrameDisplay initialized for Player {assignedPlayerId}");
        }
    }
    
    private void SetupDisplay()
    {
        if (wordDisplayText != null)
        {
            wordDisplayText.text = "";
            wordDisplayText.color = textColor;
            wordDisplayText.fontSize = fontSize;
            wordDisplayText.alignment = textAlignment;
            
            if (displayFont != null)
            {
                wordDisplayText.font = displayFont;
            }
        }
        
        if (wordDisplayImage != null)
        {
            wordDisplayImage.color = backgroundColor;
            originalMaterial = wordDisplayImage.material;
        }
        
        if (hintText != null)
        {
            hintText.text = "";
            hintText.color = hintTextColor;
            hintText.fontSize = hintFontSize;
            hintText.gameObject.SetActive(showWordHints);
        }
        
        // Set layer for visibility control
        if (onlyVisibleToDescriber)
        {
            gameObject.layer = Mathf.RoundToInt(Mathf.Log(visibilityLayer.value, 2));
        }
    }
    
    private void Update()
    {
        UpdateVisibility();
        UpdateDebugInfo();
    }
    
    private void UpdateVisibility()
    {
        bool wasCanSee = canCurrentPlayerSee;
        canCurrentPlayerSee = CanCurrentPlayerSeeFrame();
        
        if (canCurrentPlayerSee != wasCanSee)
        {
            OnVisibilityChanged(canCurrentPlayerSee);
        }
        
        // Apply privacy shader if enabled
        if (usePrivacyShader && !canCurrentPlayerSee)
        {
            ApplyPrivacyEffect();
        }
        else if (usePrivacyShader && canCurrentPlayerSee)
        {
            RemovePrivacyEffect();
        }
    }
    
    private bool CanCurrentPlayerSeeFrame()
    {
        // Check role permissions
        if (onlyVisibleToDescriber && PlayerRoleManager.Instance != null)
        {
            if (!PlayerRoleManager.Instance.CanPlayerSeePictureFrame(assignedPlayerId))
            {
                return false;
            }
        }
        
        // Check viewing angle if observer is set
        if (observerTransform != null)
        {
            Vector3 toObserver = (observerTransform.position - transform.position).normalized;
            Vector3 frameForward = transform.forward;
            float angle = Vector3.Angle(frameForward, toObserver);
            
            return angle <= viewingAngle * 0.5f;
        }
        
        return true;
    }
    
    private void OnVisibilityChanged(bool isVisible)
    {
        if (displayCanvas != null)
        {
            displayCanvas.enabled = isVisible;
        }
        
        if (frameLight != null)
        {
            frameLight.enabled = isVisible && isWordVisible;
        }
        
        if (debugMode)
        {
            Debug.Log($"Picture frame visibility changed to: {isVisible} for Player {assignedPlayerId}");
        }
    }
    
    private void HandleNewWordSelected(string newWord)
    {
        SetDisplayWord(newWord);
    }
    
    private void HandleGamePhaseChanged(WordGuessingGameManager.GamePhase newPhase)
    {
        switch (newPhase)
        {
            case WordGuessingGameManager.GamePhase.RoundStart:
                // Word should already be set, just ensure it's visible
                break;
                
            case WordGuessingGameManager.GamePhase.RoundEnd:
                // Could add a completion effect
                if (frameParticles != null)
                    frameParticles.Play();
                break;
                
            case WordGuessingGameManager.GamePhase.StageComplete:
                ClearDisplay();
                break;
        }
    }
    
    private void HandlePlayerDataUpdated(PlayerRoleManager.PlayerData playerData)
    {
        if (playerData.playerId == assignedPlayerId)
        {
            UpdateVisibility();
        }
    }
    
    private void HandlePlayerPermissionsChanged(int playerId, bool isConnected)
    {
        if (playerId == assignedPlayerId)
        {
            UpdateVisibility();
        }
    }
    
    public void SetDisplayWord(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            ClearDisplay();
            return;
        }
        
        string previousWord = currentWord;
        currentWord = word;
        currentDisplayWord = word; // For debug
        
        if (useWordRevealAnimation)
        {
            if (revealCoroutine != null)
                StopCoroutine(revealCoroutine);
            
            revealCoroutine = StartCoroutine(RevealWordAnimation());
        }
        else
        {
            DisplayWordImmediately();
        }
        
        // Play sound effect
        if (frameAudioSource != null)
        {
            AudioClip soundToPlay = string.IsNullOrEmpty(previousWord) ? wordAppearSound : wordChangeSound;
            if (soundToPlay != null)
            {
                frameAudioSource.PlayOneShot(soundToPlay);
            }
        }
        
        // Update hint if enabled
        if (showWordHints && hintText != null)
        {
            SetWordHint(word);
        }
        
        if (debugMode)
        {
            Debug.Log($"Picture frame displaying word: '{word}' for Player {assignedPlayerId}");
        }
    }
    
    private IEnumerator RevealWordAnimation()
    {
        if (wordDisplayText == null) yield break;
        
        isWordVisible = false;
        wordDisplayText.text = "";
        
        // Fade in effect
        Color startColor = textColor;
        startColor.a = 0f;
        wordDisplayText.color = startColor;
        
        float elapsedTime = 0f;
        while (elapsedTime < revealDuration)
        {
            float progress = elapsedTime / revealDuration;
            float curveValue = revealCurve.Evaluate(progress);
            
            // Reveal characters progressively
            int charactersToShow = Mathf.RoundToInt(currentWord.Length * curveValue);
            wordDisplayText.text = currentWord.Substring(0, charactersToShow);
            
            // Fade in alpha
            Color currentColor = textColor;
            currentColor.a = curveValue;
            wordDisplayText.color = currentColor;
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state
        DisplayWordImmediately();
        
        // Start shimmer effect if enabled
        if (useShimmerEffect)
        {
            if (shimmerCoroutine != null)
                StopCoroutine(shimmerCoroutine);
            shimmerCoroutine = StartCoroutine(ShimmerEffect());
        }
    }
    
    private void DisplayWordImmediately()
    {
        if (wordDisplayText != null)
        {
            wordDisplayText.text = currentWord;
            wordDisplayText.color = textColor;
        }
        
        isWordVisible = true;
        
        // Enable frame light
        if (frameLight != null && canCurrentPlayerSee)
        {
            frameLight.enabled = true;
        }
    }
    
    private IEnumerator ShimmerEffect()
    {
        if (wordDisplayText == null) yield break;
        
        while (isWordVisible)
        {
            float shimmerValue = (Mathf.Sin(Time.time * shimmerSpeed) + 1f) * 0.5f;
            Color shimmerColor = Color.Lerp(textColor, Color.white, shimmerValue * 0.3f);
            wordDisplayText.color = shimmerColor;
            
            yield return null;
        }
        
        // Restore original color
        if (wordDisplayText != null)
            wordDisplayText.color = textColor;
    }
    
    private void SetWordHint(string word)
    {
        if (hintText == null) return;
        
        WordEntry wordEntry = null;
        if (WordGuessingGameManager.Instance != null)
        {
            var wordDatabase = WordGuessingGameManager.Instance.GetComponent<WordDatabase>();
            if (wordDatabase != null)
            {
                wordEntry = wordDatabase.GetWordEntry(word);
            }
        }
        
        string hint = wordEntry?.hint ?? $"A {word.Length}-letter word";
        hintText.text = $"Hint: {hint}";
    }
    
    private void ApplyPrivacyEffect()
    {
        if (wordDisplayImage != null && privacyMaterial != null)
        {
            wordDisplayImage.material = privacyMaterial;
        }
        
        if (wordDisplayText != null)
        {
            Color hiddenColor = textColor;
            hiddenColor.a = 0.1f;
            wordDisplayText.color = hiddenColor;
        }
    }
    
    private void RemovePrivacyEffect()
    {
        if (wordDisplayImage != null && originalMaterial != null)
        {
            wordDisplayImage.material = originalMaterial;
        }
        
        if (wordDisplayText != null)
        {
            wordDisplayText.color = textColor;
        }
    }
    
    public void ClearDisplay()
    {
        currentWord = "";
        currentDisplayWord = "";
        isWordVisible = false;
        
        if (wordDisplayText != null)
        {
            wordDisplayText.text = "";
        }
        
        if (hintText != null)
        {
            hintText.text = "";
        }
        
        if (frameLight != null)
        {
            frameLight.enabled = false;
        }
        
        // Stop animations
        if (revealCoroutine != null)
        {
            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
        }
        
        if (shimmerCoroutine != null)
        {
            StopCoroutine(shimmerCoroutine);
            shimmerCoroutine = null;
        }
        
        if (debugMode)
        {
            Debug.Log($"Picture frame display cleared for Player {assignedPlayerId}");
        }
    }
    
    private void UpdateDebugInfo()
    {
        if (showDebugInfo && debugMode)
        {
            currentDisplayWord = currentWord;
        }
    }
    
    public void SetAssignedPlayer(int playerId)
    {
        assignedPlayerId = playerId;
        UpdateVisibility();
        
        if (debugMode)
        {
            Debug.Log($"Picture frame assigned to Player {playerId}");
        }
    }
    
    public void SetObserverTransform(Transform observer)
    {
        observerTransform = observer;
        
        if (debugMode)
        {
            Debug.Log($"Observer transform set for picture frame");
        }
    }
    
    public void SetTextProperties(Color color, int size, TextAlignmentOptions alignment)
    {
        textColor = color;
        fontSize = size;
        textAlignment = alignment;
        
        if (wordDisplayText != null)
        {
            wordDisplayText.color = textColor;
            wordDisplayText.fontSize = fontSize;
            wordDisplayText.alignment = textAlignment;
        }
        
        if (debugMode)
        {
            Debug.Log("Picture frame text properties updated");
        }
    }
    
    public string GetCurrentWord()
    {
        return currentWord;
    }
    
    public bool IsWordVisible()
    {
        return isWordVisible;
    }
    
    public bool CanPlayerSeeFrame()
    {
        return canCurrentPlayerSee;
    }
    
    public int GetAssignedPlayer()
    {
        return assignedPlayerId;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (observerTransform != null)
        {
            // Draw viewing angle
            Vector3 forward = transform.forward;
            Vector3 left = Quaternion.AngleAxis(-viewingAngle * 0.5f, transform.up) * forward;
            Vector3 right = Quaternion.AngleAxis(viewingAngle * 0.5f, transform.up) * forward;
            
            Gizmos.color = canCurrentPlayerSee ? Color.green : Color.red;
            Gizmos.DrawRay(transform.position, left * 2f);
            Gizmos.DrawRay(transform.position, right * 2f);
            
            // Draw line to observer
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, observerTransform.position);
        }
    }
}