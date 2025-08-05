using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections;

public class RoomFeedbackController : MonoBehaviour
{
    [Header("Room Lighting")]
    [SerializeField] private Light[] roomLights;
    [SerializeField] private bool autoFindLights = true;
    [SerializeField] private LayerMask lightLayerMask = -1;
    
    [Header("Feedback Colors")]
    [SerializeField] private Color positiveColor = Color.blue;
    [SerializeField] private Color negativeColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private float feedbackIntensity = 2f;
    
    [Header("Room Materials")]
    [SerializeField] private Renderer[] roomRenderers;
    [SerializeField] private Material positiveMaterial;
    [SerializeField] private Material negativeMaterial;
    [SerializeField] private Material neutralMaterial;
    [SerializeField] private bool changeMaterials = false;
    
    [Header("Environment Effects")]
    [SerializeField] private ParticleSystem positiveParticles;
    [SerializeField] private ParticleSystem negativeParticles;
    [SerializeField] private AudioSource feedbackAudioSource;
    [SerializeField] private AudioClip positiveSound;
    [SerializeField] private AudioClip negativeSound;
    
    [Header("Animation Settings")]
    [SerializeField] private float transitionDuration = 0.5f;
    [SerializeField] private float pulseDuration = 0.3f;
    [SerializeField] private AnimationCurve feedbackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private bool usePulseEffect = true;
    
    [Header("Room Assignment")]
    [SerializeField] private int assignedPlayerId = 2; // Default to describer
    [SerializeField] private bool affectBothRooms = false;
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool visualizeEffectZone = true;
    [SerializeField] private float effectRadius = 10f;
    
    private Color originalLightColor;
    private float originalLightIntensity;
    private Material[] originalMaterials;
    private bool isShowingFeedback = false;
    private Coroutine currentFeedbackCoroutine;
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        StoreSOriginalSettings();
    }
    
    private void OnEnable()
    {
        PlayerRoleManager.OnPlayerDataUpdated += HandlePlayerDataUpdated;
    }
    
    private void OnDisable()
    {
        PlayerRoleManager.OnPlayerDataUpdated -= HandlePlayerDataUpdated;
    }
    
    private void InitializeComponents()
    {
        if (autoFindLights && (roomLights == null || roomLights.Length == 0))
        {
            FindRoomLights();
        }
        
        if (roomRenderers == null || roomRenderers.Length == 0)
        {
            FindRoomRenderers();
        }
        
        if (feedbackAudioSource == null)
        {
            feedbackAudioSource = GetComponent<AudioSource>();
            if (feedbackAudioSource == null)
            {
                feedbackAudioSource = gameObject.AddComponent<AudioSource>();
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"RoomFeedbackController initialized for Player {assignedPlayerId}");
        }
    }
    
    private void FindRoomLights()
    {
        Light[] allLights = FindObjectsByType<Light>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Light> validLights = new System.Collections.Generic.List<Light>();
        
        foreach (Light light in allLights)
        {
            if (IsLightInRange(light))
            {
                validLights.Add(light);
            }
        }
        
        roomLights = validLights.ToArray();
        
        if (debugMode)
        {
            Debug.Log($"Found {roomLights.Length} lights for room feedback");
        }
    }
    
    private bool IsLightInRange(Light light)
    {
        if (affectBothRooms) return true;
        
        float distance = Vector3.Distance(transform.position, light.transform.position);
        return distance <= effectRadius;
    }
    
    private void FindRoomRenderers()
    {
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        System.Collections.Generic.List<Renderer> validRenderers = new System.Collections.Generic.List<Renderer>();
        
        foreach (Renderer renderer in allRenderers)
        {
            if (IsRendererInRange(renderer) && ShouldAffectRenderer(renderer))
            {
                validRenderers.Add(renderer);
            }
        }
        
        roomRenderers = validRenderers.ToArray();
        
        if (debugMode)
        {
            Debug.Log($"Found {roomRenderers.Length} renderers for room feedback");
        }
    }
    
    private bool IsRendererInRange(Renderer renderer)
    {
        if (affectBothRooms) return true;
        
        float distance = Vector3.Distance(transform.position, renderer.transform.position);
        return distance <= effectRadius;
    }
    
    private bool ShouldAffectRenderer(Renderer renderer)
    {
        // Exclude certain objects like players, UI, or other interactive elements
        if (renderer.CompareTag("Player") || 
            renderer.CompareTag("UI") || 
            renderer.CompareTag("Interactable"))
        {
            return false;
        }
        
        // Include walls, floors, ceiling, etc.
        return renderer.CompareTag("Wall") || 
               renderer.CompareTag("Floor") || 
               renderer.CompareTag("Environment") ||
               renderer.name.ToLower().Contains("wall") ||
               renderer.name.ToLower().Contains("floor") ||
               renderer.name.ToLower().Contains("ceiling");
    }
    
    private void StoreSOriginalSettings()
    {
        // Store original light settings
        if (roomLights != null && roomLights.Length > 0)
        {
            originalLightColor = roomLights[0].color;
            originalLightIntensity = roomLights[0].intensity;
        }
        
        // Store original materials
        if (roomRenderers != null && roomRenderers.Length > 0)
        {
            originalMaterials = new Material[roomRenderers.Length];
            for (int i = 0; i < roomRenderers.Length; i++)
            {
                if (roomRenderers[i] != null)
                {
                    originalMaterials[i] = roomRenderers[i].material;
                }
            }
        }
    }
    
    public void ShowPositiveFeedback(float duration = 2f)
    {
        ShowFeedback(true, duration);
    }
    
    public void ShowNegativeFeedback(float duration = 2f)
    {
        ShowFeedback(false, duration);
    }
    
    private void ShowFeedback(bool isPositive, float duration)
    {
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
        }
        
        currentFeedbackCoroutine = StartCoroutine(FeedbackSequence(isPositive, duration));
        
        if (debugMode)
        {
            Debug.Log($"Showing {(isPositive ? "positive" : "negative")} feedback for {duration} seconds");
        }
    }
    
    private IEnumerator FeedbackSequence(bool isPositive, float duration)
    {
        isShowingFeedback = true;
        
        Color targetColor = isPositive ? positiveColor : negativeColor;
        Material targetMaterial = isPositive ? positiveMaterial : negativeMaterial;
        AudioClip targetSound = isPositive ? positiveSound : negativeSound;
        ParticleSystem targetParticles = isPositive ? positiveParticles : negativeParticles;
        
        // Play sound effect
        if (feedbackAudioSource != null && targetSound != null)
        {
            feedbackAudioSource.PlayOneShot(targetSound);
        }
        
        // Play particles
        if (targetParticles != null)
        {
            targetParticles.Play();
        }
        
        // Transition to feedback state
        yield return StartCoroutine(TransitionToFeedback(targetColor, targetMaterial));
        
        // Apply pulse effect if enabled
        if (usePulseEffect)
        {
            yield return StartCoroutine(PulseEffect(targetColor, pulseDuration));
        }
        
        // Hold feedback state
        float holdDuration = duration - transitionDuration - (usePulseEffect ? pulseDuration : 0);
        if (holdDuration > 0)
        {
            yield return new WaitForSeconds(holdDuration);
        }
        
        // Transition back to normal
        yield return StartCoroutine(TransitionToNormal());
        
        isShowingFeedback = false;
        currentFeedbackCoroutine = null;
    }
    
    private IEnumerator TransitionToFeedback(Color targetColor, Material targetMaterial)
    {
        float elapsedTime = 0f;
        
        Color startLightColor = roomLights != null && roomLights.Length > 0 ? roomLights[0].color : Color.white;
        float startLightIntensity = roomLights != null && roomLights.Length > 0 ? roomLights[0].intensity : 1f;
        
        while (elapsedTime < transitionDuration)
        {
            float progress = elapsedTime / transitionDuration;
            float curveValue = feedbackCurve.Evaluate(progress);
            
            // Update lights
            UpdateLights(
                Color.Lerp(startLightColor, targetColor, curveValue),
                Mathf.Lerp(startLightIntensity, feedbackIntensity, curveValue)
            );
            
            // Update materials if enabled
            if (changeMaterials && targetMaterial != null)
            {
                UpdateMaterials(targetMaterial, curveValue);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state
        UpdateLights(targetColor, feedbackIntensity);
        if (changeMaterials && targetMaterial != null)
        {
            UpdateMaterials(targetMaterial, 1f);
        }
    }
    
    private IEnumerator PulseEffect(Color baseColor, float pulseDuration)
    {
        float elapsedTime = 0f;
        
        while (elapsedTime < pulseDuration)
        {
            float progress = elapsedTime / pulseDuration;
            float pulseValue = Mathf.Sin(progress * Mathf.PI * 4); // 4 pulses
            
            float intensity = feedbackIntensity + (pulseValue * feedbackIntensity * 0.5f);
            UpdateLights(baseColor, intensity);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }
    
    private IEnumerator TransitionToNormal()
    {
        float elapsedTime = 0f;
        
        Color startLightColor = roomLights != null && roomLights.Length > 0 ? roomLights[0].color : Color.white;
        float startLightIntensity = roomLights != null && roomLights.Length > 0 ? roomLights[0].intensity : 1f;
        
        while (elapsedTime < transitionDuration)
        {
            float progress = elapsedTime / transitionDuration;
            float curveValue = feedbackCurve.Evaluate(progress);
            
            // Update lights
            UpdateLights(
                Color.Lerp(startLightColor, originalLightColor, curveValue),
                Mathf.Lerp(startLightIntensity, originalLightIntensity, curveValue)
            );
            
            // Update materials
            if (changeMaterials)
            {
                UpdateMaterialsToOriginal(curveValue);
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure final state
        UpdateLights(originalLightColor, originalLightIntensity);
        if (changeMaterials)
        {
            RestoreOriginalMaterials();
        }
    }
    
    private void UpdateLights(Color color, float intensity)
    {
        if (roomLights == null) return;
        
        foreach (Light light in roomLights)
        {
            if (light != null)
            {
                light.color = color;
                light.intensity = intensity;
            }
        }
    }
    
    private void UpdateMaterials(Material targetMaterial, float blendFactor)
    {
        if (roomRenderers == null || targetMaterial == null) return;
        
        foreach (Renderer renderer in roomRenderers)
        {
            if (renderer != null)
            {
                // For simplicity, we'll just set the material
                // In a more complex system, you might want to blend colors
                renderer.material = targetMaterial;
            }
        }
    }
    
    private void UpdateMaterialsToOriginal(float blendFactor)
    {
        if (roomRenderers == null || originalMaterials == null) return;
        
        for (int i = 0; i < roomRenderers.Length; i++)
        {
            if (roomRenderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
            {
                roomRenderers[i].material = originalMaterials[i];
            }
        }
    }
    
    private void RestoreOriginalMaterials()
    {
        if (roomRenderers == null || originalMaterials == null) return;
        
        for (int i = 0; i < roomRenderers.Length; i++)
        {
            if (roomRenderers[i] != null && i < originalMaterials.Length && originalMaterials[i] != null)
            {
                roomRenderers[i].material = originalMaterials[i];
            }
        }
    }
    
    private void HandlePlayerDataUpdated(PlayerRoleManager.PlayerData playerData)
    {
        if (playerData.playerId == assignedPlayerId)
        {
            // Could adjust feedback based on player state if needed
        }
    }
    
    public void SetAssignedPlayer(int playerId)
    {
        assignedPlayerId = playerId;
        
        if (debugMode)
        {
            Debug.Log($"Room feedback controller assigned to Player {playerId}");
        }
    }
    
    public void SetFeedbackColors(Color positive, Color negative, Color neutral)
    {
        positiveColor = positive;
        negativeColor = negative;
        neutralColor = neutral;
        
        if (debugMode)
        {
            Debug.Log("Feedback colors updated");
        }
    }
    
    public void SetFeedbackIntensity(float intensity)
    {
        feedbackIntensity = Mathf.Max(0f, intensity);
        
        if (debugMode)
        {
            Debug.Log($"Feedback intensity set to: {feedbackIntensity}");
        }
    }
    
    public void StopFeedback()
    {
        if (currentFeedbackCoroutine != null)
        {
            StopCoroutine(currentFeedbackCoroutine);
            currentFeedbackCoroutine = null;
        }
        
        UpdateLights(originalLightColor, originalLightIntensity);
        if (changeMaterials)
        {
            RestoreOriginalMaterials();
        }
        
        isShowingFeedback = false;
        
        if (debugMode)
        {
            Debug.Log("Feedback stopped and room restored to normal");
        }
    }
    
    public bool IsShowingFeedback()
    {
        return isShowingFeedback;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (visualizeEffectZone)
        {
            Gizmos.color = isShowingFeedback ? Color.yellow : Color.cyan;
            Gizmos.DrawWireSphere(transform.position, effectRadius);
        }
    }
}