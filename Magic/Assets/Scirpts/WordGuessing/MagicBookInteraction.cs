using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class MagicBookInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private int assignedPlayerId = 1;
    [SerializeField] private float activationDistance = 0.5f;
    [SerializeField] private bool requireHandContact = true;
    [SerializeField] private LayerMask handLayerMask = -1;
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject highlightEffect;
    [SerializeField] private ParticleSystem activationParticles;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip deactivationSound;
    
    [Header("Animation")]
    [SerializeField] private Animator bookAnimator;
    [SerializeField] private string activationTrigger = "Activate";
    [SerializeField] private string deactivationTrigger = "Deactivate";
    [SerializeField] private bool useHoverAnimation = true;
    
    [Header("Magic Book Display")]
    [SerializeField] private MagicBookTextDisplay textDisplay;
    [SerializeField] private bool autoFindTextDisplay = true;
    
    [Header("Permissions")]
    [SerializeField] private bool enforceRolePermissions = true;
    [SerializeField] private bool showPermissionDeniedFeedback = true;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool visualizeActivationZone = true;
    
    private bool isActivated = false;
    private bool isPlayerNearby = false;
    private bool canPlayerInteract = false;
    private Transform currentInteractingHand;
    private Coroutine activationCoroutine;
    
    // XR Components
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socketInteractor;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        SetupInteractionComponents();
        UpdateInteractionState();
    }
    
    private void OnEnable()
    {
        PlayerRoleManager.OnPlayerPermissionsChanged += HandlePlayerPermissionsChanged;
        WordGuessingGameManager.OnGamePhaseChanged += HandleGamePhaseChanged;
        WordGuessingVoiceHandler.OnVoiceActivated += HandleVoiceActivated;
        WordGuessingVoiceHandler.OnVoiceDeactivated += HandleVoiceDeactivated;
    }
    
    private void OnDisable()
    {
        PlayerRoleManager.OnPlayerPermissionsChanged -= HandlePlayerPermissionsChanged;
        WordGuessingGameManager.OnGamePhaseChanged -= HandleGamePhaseChanged;
        WordGuessingVoiceHandler.OnVoiceActivated -= HandleVoiceActivated;
        WordGuessingVoiceHandler.OnVoiceDeactivated -= HandleVoiceDeactivated;
    }
    
    private void InitializeComponents()
    {
        if (autoFindTextDisplay && textDisplay == null)
        {
            textDisplay = GetComponentInChildren<MagicBookTextDisplay>();
            if (textDisplay == null)
                textDisplay = FindFirstObjectByType<MagicBookTextDisplay>();
        }
        
        if (highlightEffect != null)
            highlightEffect.SetActive(false);
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (bookAnimator == null)
            bookAnimator = GetComponent<Animator>();
        
        if (debugMode)
        {
            Debug.Log($"MagicBookInteraction initialized for Player {assignedPlayerId}");
        }
    }
    
    private void SetupInteractionComponents()
    {
        // Setup XR Grab Interactable if not present
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null && requireHandContact)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        }
        
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnHandSelectEntered);
            grabInteractable.selectExited.AddListener(OnHandSelectExited);
            grabInteractable.hoverEntered.AddListener(OnHandHoverEntered);
            grabInteractable.hoverExited.AddListener(OnHandHoverExited);
        }
        
        // Ensure proper collider setup
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    private void Update()
    {
        UpdatePlayerProximity();
        UpdateInteractionState();
    }
    
    private void UpdatePlayerProximity()
    {
        // Check if assigned player is nearby
        bool wasNearby = isPlayerNearby;
        isPlayerNearby = IsAssignedPlayerNearby();
        
        if (isPlayerNearby != wasNearby)
        {
            OnPlayerProximityChanged(isPlayerNearby);
        }
    }
    
    private bool IsAssignedPlayerNearby()
    {
        // In a real implementation, you'd check player position
        // For now, we'll simulate this or check based on hand positions
        
        if (Camera.main != null)
        {
            float distance = Vector3.Distance(transform.position, Camera.main.transform.position);
            return distance <= activationDistance;
        }
        
        return false;
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
        if (!isPlayerNearby) return false;
        
        if (enforceRolePermissions && PlayerRoleManager.Instance != null)
        {
            return PlayerRoleManager.Instance.CanPlayerUseMagicBook(assignedPlayerId);
        }
        
        return true;
    }
    
    private void OnPlayerProximityChanged(bool isNear)
    {
        if (isNear)
        {
            OnPlayerEnterActivationZone();
        }
        else
        {
            OnPlayerExitActivationZone();
        }
    }
    
    private void OnPlayerEnterActivationZone()
    {
        if (useHoverAnimation && bookAnimator != null)
        {
            bookAnimator.SetBool("IsPlayerNearby", true);
        }
        
        UpdateVisualFeedback();
        
        if (debugMode)
        {
            Debug.Log($"Player {assignedPlayerId} entered magic book activation zone");
        }
    }
    
    private void OnPlayerExitActivationZone()
    {
        if (isActivated)
        {
            DeactivateMagicBook();
        }
        
        if (useHoverAnimation && bookAnimator != null)
        {
            bookAnimator.SetBool("IsPlayerNearby", false);
        }
        
        UpdateVisualFeedback();
        
        if (debugMode)
        {
            Debug.Log($"Player {assignedPlayerId} exited magic book activation zone");
        }
    }
    
    private void OnHandSelectEntered(SelectEnterEventArgs args)
    {
        currentInteractingHand = args.interactorObject.transform;
        
        if (canPlayerInteract)
        {
            ActivateMagicBook();
        }
        else if (showPermissionDeniedFeedback)
        {
            ShowPermissionDeniedFeedback();
        }
        
        if (debugMode)
        {
            Debug.Log($"Hand placed on magic book - Player {assignedPlayerId}");
        }
    }
    
    private void OnHandSelectExited(SelectExitEventArgs args)
    {
        currentInteractingHand = null;
        
        if (isActivated)
        {
            DeactivateMagicBook();
        }
        
        if (debugMode)
        {
            Debug.Log($"Hand removed from magic book - Player {assignedPlayerId}");
        }
    }
    
    private void OnHandHoverEntered(HoverEnterEventArgs args)
    {
        if (canPlayerInteract && highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }
    }
    
    private void OnHandHoverExited(HoverExitEventArgs args)
    {
        if (highlightEffect != null && !isActivated)
        {
            highlightEffect.SetActive(false);
        }
    }
    
    public void ActivateMagicBook()
    {
        if (isActivated || !canPlayerInteract) return;
        
        isActivated = true;
        
        // Visual and audio feedback
        PlayActivationEffects();
        
        // Activate voice recognition for this player
        if (WordGuessingVoiceHandler.Instance != null)
        {
            WordGuessingVoiceHandler.Instance.ActivateVoiceForPlayer(assignedPlayerId);
        }
        
        // Update text display
        if (textDisplay != null)
        {
            textDisplay.ShowTestMessage("Magic Book activated! Speak to communicate.");
        }
        
        if (debugMode)
        {
            Debug.Log($"Magic Book activated for Player {assignedPlayerId}");
        }
    }
    
    public void DeactivateMagicBook()
    {
        if (!isActivated) return;
        
        isActivated = false;
        
        // Visual and audio feedback
        PlayDeactivationEffects();
        
        // Deactivate voice recognition
        if (WordGuessingVoiceHandler.Instance != null)
        {
            WordGuessingVoiceHandler.Instance.DeactivateVoiceForPlayer(assignedPlayerId);
        }
        
        // Update text display
        if (textDisplay != null)
        {
            textDisplay.ShowTestMessage("Magic Book deactivated.");
        }
        
        if (debugMode)
        {
            Debug.Log($"Magic Book deactivated for Player {assignedPlayerId}");
        }
    }
    
    private void PlayActivationEffects()
    {
        // Particle effect
        if (activationParticles != null)
        {
            activationParticles.Play();
        }
        
        // Sound effect
        if (audioSource != null && activationSound != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Animation
        if (bookAnimator != null && !string.IsNullOrEmpty(activationTrigger))
        {
            bookAnimator.SetTrigger(activationTrigger);
        }
        
        // Highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(true);
        }
    }
    
    private void PlayDeactivationEffects()
    {
        // Sound effect
        if (audioSource != null && deactivationSound != null)
        {
            audioSource.PlayOneShot(deactivationSound);
        }
        
        // Animation
        if (bookAnimator != null && !string.IsNullOrEmpty(deactivationTrigger))
        {
            bookAnimator.SetTrigger(deactivationTrigger);
        }
        
        // Highlight effect
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(false);
        }
    }
    
    private void ShowPermissionDeniedFeedback()
    {
        if (textDisplay != null)
        {
            textDisplay.ShowTestMessage("You cannot use the Magic Book in your current role!");
        }
        
        // Could add red flash effect or error sound here
        
        if (debugMode)
        {
            Debug.Log($"Magic Book interaction denied for Player {assignedPlayerId} - insufficient permissions");
        }
    }
    
    private void UpdateVisualFeedback()
    {
        if (highlightEffect != null)
        {
            bool shouldHighlight = canPlayerInteract && (isPlayerNearby || isActivated);
            highlightEffect.SetActive(shouldHighlight);
        }
    }
    
    private void HandlePlayerPermissionsChanged(int playerId, bool isConnected)
    {
        if (playerId == assignedPlayerId)
        {
            UpdateInteractionState();
            
            // If player lost permissions while activated, deactivate
            if (isActivated && !CanPlayerCurrentlyInteract())
            {
                DeactivateMagicBook();
            }
        }
    }
    
    private void HandleGamePhaseChanged(WordGuessingGameManager.GamePhase newPhase)
    {
        // Auto-deactivate during certain phases
        if (newPhase == WordGuessingGameManager.GamePhase.RoundEnd ||
            newPhase == WordGuessingGameManager.GamePhase.StageComplete)
        {
            if (isActivated)
            {
                DeactivateMagicBook();
            }
        }
    }
    
    private void HandleVoiceActivated(int playerId)
    {
        if (playerId == assignedPlayerId && textDisplay != null)
        {
            textDisplay.ShowTestMessage("Voice recognition active - speak now!");
        }
    }
    
    private void HandleVoiceDeactivated(int playerId)
    {
        if (playerId == assignedPlayerId && textDisplay != null)
        {
            textDisplay.ShowTestMessage("Voice recognition stopped.");
        }
    }
    
    public void SetAssignedPlayer(int playerId)
    {
        assignedPlayerId = playerId;
        
        if (textDisplay != null)
        {
            textDisplay.SetAssignedPlayer(playerId);
        }
        
        if (debugMode)
        {
            Debug.Log($"Magic Book assigned to Player {playerId}");
        }
    }
    
    public void SetTextDisplay(MagicBookTextDisplay display)
    {
        textDisplay = display;
        
        if (display != null)
        {
            display.SetAssignedPlayer(assignedPlayerId);
        }
    }
    
    public bool IsActivated()
    {
        return isActivated;
    }
    
    public int GetAssignedPlayer()
    {
        return assignedPlayerId;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (visualizeActivationZone)
        {
            Gizmos.color = canPlayerInteract ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, activationDistance);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Alternative trigger-based interaction for non-XR setups
        if (!requireHandContact)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnterActivationZone();
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (!requireHandContact)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerExitActivationZone();
            }
        }
    }
}