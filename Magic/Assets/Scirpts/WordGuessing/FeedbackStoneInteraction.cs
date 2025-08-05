using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

public class FeedbackStoneInteraction : MonoBehaviour
{
    public enum StoneType
    {
        Blue,   // Positive/Yes feedback
        Red     // Negative/No feedback
    }
    
    [Header("Stone Configuration")]
    [SerializeField] private StoneType stoneType = StoneType.Blue;
    [SerializeField] private int assignedPlayerId = 2; // Default to Player 2 (describer)
    [SerializeField] private float activationCooldown = 1f;
    
    [Header("Visual Effects")]
    [SerializeField] private Renderer stoneRenderer;
    [SerializeField] private Material normalMaterial;
    [SerializeField] private Material activatedMaterial;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private Light stoneLight;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip deniedSound;
    
    [Header("Room Feedback")]
    [SerializeField] private RoomFeedbackController roomFeedback;
    [SerializeField] private bool autoFindRoomFeedback = true;
    [SerializeField] private float feedbackDuration = 2f;
    
    [Header("Animation")]
    [SerializeField] private Animator stoneAnimator;
    [SerializeField] private string activationTrigger = "Activate";
    [SerializeField] private bool useFloatingAnimation = true;
    [SerializeField] private float floatAmplitude = 0.1f;
    [SerializeField] private float floatSpeed = 2f;
    
    [Header("Interaction Settings")]
    [SerializeField] private bool requireGrab = true;
    [SerializeField] private bool requireRolePermission = true;
    [SerializeField] private bool showPermissionFeedback = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    
    private bool isActivated = false;
    private bool isOnCooldown = false;
    private bool canPlayerInteract = false;
    private Vector3 originalPosition;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private float lastActivationTime = 0f;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        SetupInteraction();
        UpdateInteractionState();
        originalPosition = transform.localPosition;
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
    
    private void InitializeComponents()
    {
        if (stoneRenderer == null)
            stoneRenderer = GetComponent<Renderer>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (stoneAnimator == null)
            stoneAnimator = GetComponent<Animator>();
        
        if (autoFindRoomFeedback && roomFeedback == null)
            roomFeedback = FindFirstObjectByType<RoomFeedbackController>();
        
        if (stoneLight == null)
            stoneLight = GetComponentInChildren<Light>();
        
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone initialized for Player {assignedPlayerId}");
        }
    }
    
    private void SetupInteraction()
    {
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null && requireGrab)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
            grabInteractable.activated.AddListener(OnActivated);
        }
        
        // Ensure collider for interaction
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            col = gameObject.AddComponent<SphereCollider>();
        }
    }
    
    private void Update()
    {
        UpdateInteractionState();
        UpdateFloatingAnimation();
        UpdateCooldown();
    }
    
    private void UpdateInteractionState()
    {
        bool wasCanInteract = canPlayerInteract;
        canPlayerInteract = CanPlayerCurrentlyInteract();
        
        if (canPlayerInteract != wasCanInteract)
        {
            UpdateVisualFeedback();
        }
    }
    
    private bool CanPlayerCurrentlyInteract()
    {
        if (isOnCooldown) return false;
        
        if (requireRolePermission && PlayerRoleManager.Instance != null)
        {
            return PlayerRoleManager.Instance.CanPlayerUseStones(assignedPlayerId);
        }
        
        return true;
    }
    
    private void UpdateFloatingAnimation()
    {
        if (useFloatingAnimation && !isActivated)
        {
            float newY = originalPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.localPosition = new Vector3(originalPosition.x, newY, originalPosition.z);
        }
    }
    
    private void UpdateCooldown()
    {
        if (isOnCooldown && Time.time - lastActivationTime >= activationCooldown)
        {
            isOnCooldown = false;
            UpdateVisualFeedback();
        }
    }
    
    private void OnGrabbed(SelectEnterEventArgs args)
    {
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone grabbed by Player {assignedPlayerId}");
        }
    }
    
    private void OnReleased(SelectExitEventArgs args)
    {
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone released by Player {assignedPlayerId}");
        }
    }
    
    private void OnActivated(ActivateEventArgs args)
    {
        AttemptActivation();
    }
    
    public void AttemptActivation()
    {
        if (!canPlayerInteract)
        {
            if (showPermissionFeedback)
            {
                ShowPermissionDeniedFeedback();
            }
            return;
        }
        
        ActivateStone();
    }
    
    private void ActivateStone()
    {
        if (isActivated || isOnCooldown) return;
        
        isActivated = true;
        isOnCooldown = true;
        lastActivationTime = Time.time;
        
        // Play effects
        PlayActivationEffects();
        
        // Trigger room feedback
        TriggerRoomFeedback();
        
        // Send feedback to game system
        SendFeedbackToGame();
        
        // Auto-deactivate after duration
        StartCoroutine(DeactivateAfterDuration());
        
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone activated by Player {assignedPlayerId} - " +
                     $"Feedback: {(stoneType == StoneType.Blue ? "YES" : "NO")}");
        }
    }
    
    private void PlayActivationEffects()
    {
        // Visual effects
        if (stoneRenderer != null && activatedMaterial != null)
        {
            stoneRenderer.material = activatedMaterial;
        }
        
        if (activationParticles != null)
        {
            activationParticles.Play();
        }
        
        if (stoneLight != null)
        {
            stoneLight.enabled = true;
            stoneLight.color = stoneType == StoneType.Blue ? Color.blue : Color.red;
        }
        
        // Audio effect
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Animation
        if (stoneAnimator != null && !string.IsNullOrEmpty(activationTrigger))
        {
            stoneAnimator.SetTrigger(activationTrigger);
        }
    }
    
    private void TriggerRoomFeedback()
    {
        if (roomFeedback != null)
        {
            if (stoneType == StoneType.Blue)
            {
                roomFeedback.ShowPositiveFeedback(feedbackDuration);
            }
            else
            {
                roomFeedback.ShowNegativeFeedback(feedbackDuration);
            }
        }
    }
    
    private void SendFeedbackToGame()
    {
        // This would integrate with your game logic to handle the yes/no feedback
        bool isPositive = stoneType == StoneType.Blue;
        
        // You could create a feedback event system here
        // For example: OnFeedbackGiven?.Invoke(assignedPlayerId, isPositive);
        
        if (WordGuessingGameManager.Instance != null)
        {
            // Set game phase to feedback if appropriate
            var currentPhase = WordGuessingGameManager.Instance.GetCurrentPhase();
            if (currentPhase == WordGuessingGameManager.GamePhase.QuestionPhase)
            {
                WordGuessingGameManager.Instance.SetGamePhase(WordGuessingGameManager.GamePhase.FeedbackPhase);
            }
        }
    }
    
    private IEnumerator DeactivateAfterDuration()
    {
        yield return new WaitForSeconds(feedbackDuration);
        DeactivateStone();
    }
    
    private void DeactivateStone()
    {
        isActivated = false;
        
        // Reset visual effects
        if (stoneRenderer != null && normalMaterial != null)
        {
            stoneRenderer.material = normalMaterial;
        }
        
        if (stoneLight != null)
        {
            stoneLight.enabled = false;
        }
        
        UpdateVisualFeedback();
        
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone deactivated");
        }
    }
    
    private void ShowPermissionDeniedFeedback()
    {
        // Play denied sound
        if (audioSource != null && deniedSound != null)
        {
            audioSource.PlayOneShot(deniedSound);
        }
        
        // Could add visual feedback like red flash
        StartCoroutine(FlashDeniedFeedback());
        
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone activation denied for Player {assignedPlayerId} - insufficient permissions");
        }
    }
    
    private IEnumerator FlashDeniedFeedback()
    {
        if (stoneRenderer != null)
        {
            Material originalMaterial = stoneRenderer.material;
            Color originalColor = stoneRenderer.material.color;
            
            // Flash red briefly
            stoneRenderer.material.color = Color.red;
            yield return new WaitForSeconds(0.2f);
            stoneRenderer.material.color = originalColor;
        }
    }
    
    private void UpdateVisualFeedback()
    {
        if (stoneRenderer != null)
        {
            Color baseColor = stoneType == StoneType.Blue ? Color.blue : Color.red;
            
            if (isActivated)
            {
                stoneRenderer.material.color = baseColor;
            }
            else if (canPlayerInteract)
            {
                stoneRenderer.material.color = Color.Lerp(baseColor, Color.white, 0.3f);
            }
            else
            {
                stoneRenderer.material.color = Color.Lerp(baseColor, Color.gray, 0.5f);
            }
        }
        
        // Update light intensity based on state
        if (stoneLight != null && !isActivated)
        {
            stoneLight.enabled = canPlayerInteract;
            if (stoneLight.enabled)
            {
                stoneLight.intensity = canPlayerInteract ? 0.5f : 0.2f;
            }
        }
    }
    
    private void HandlePlayerPermissionsChanged(int playerId, bool isConnected)
    {
        if (playerId == assignedPlayerId)
        {
            UpdateInteractionState();
        }
    }
    
    private void HandleGamePhaseChanged(WordGuessingGameManager.GamePhase newPhase)
    {
        // Reset stones at certain phases
        if (newPhase == WordGuessingGameManager.GamePhase.RoundStart ||
            newPhase == WordGuessingGameManager.GamePhase.RoundEnd)
        {
            if (isActivated)
            {
                DeactivateStone();
            }
            isOnCooldown = false;
        }
    }
    
    public void SetStoneType(StoneType type)
    {
        stoneType = type;
        UpdateVisualFeedback();
        
        if (debugMode)
        {
            Debug.Log($"Stone type set to: {type}");
        }
    }
    
    public void SetAssignedPlayer(int playerId)
    {
        assignedPlayerId = playerId;
        
        if (debugMode)
        {
            Debug.Log($"{stoneType} Stone assigned to Player {playerId}");
        }
    }
    
    public void SetRoomFeedbackController(RoomFeedbackController controller)
    {
        roomFeedback = controller;
        
        if (debugMode)
        {
            Debug.Log($"Room feedback controller set for {stoneType} Stone");
        }
    }
    
    public bool IsActivated()
    {
        return isActivated;
    }
    
    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
    
    public StoneType GetStoneType()
    {
        return stoneType;
    }
    
    public int GetAssignedPlayer()
    {
        return assignedPlayerId;
    }
    
    // Alternative activation method for non-VR setups
    private void OnMouseDown()
    {
        if (!requireGrab)
        {
            AttemptActivation();
        }
    }
}