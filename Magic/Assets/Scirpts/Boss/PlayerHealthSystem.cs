using UnityEngine;
using System.Collections;
using System;

public class PlayerHealthSystem : MonoBehaviour
{
    [System.Serializable]
    public class HealthConfig
    {
        public int maxHealth = 100;
        public int currentHealth = 100;
        public float regenRate = 5f;
        public float regenDelay = 3f;
        public bool canRegenerate = true;
        public float invulnerabilityDuration = 1f;
        public bool isInvulnerable = false;
    }
    
    public enum HealthState
    {
        Healthy,
        Injured,
        Critical,
        Dead,
        Regenerating
    }
    
    [Header("Health Configuration")]
    [SerializeField] private HealthConfig config = new HealthConfig();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float criticalHealthThreshold = 0.25f;
    [SerializeField] private float injuredHealthThreshold = 0.5f;
    
    [Header("Visual Effects")]    
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject healingEffect;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private GameObject criticalHealthEffect;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip healingSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip heartbeatSound;
    [SerializeField] private AudioClip criticalWarningSound;
    
    [Header("Screen Effects")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool enableScreenEffects = true;
    
    private HealthState currentState = HealthState.Healthy;
    private Coroutine regenCoroutine;
    private Coroutine invulnerabilityCoroutine;
    private Coroutine criticalStateCoroutine;
    private float lastDamageTime = 0f;
    private bool isRegenerating = false;
    
    public static event Action<int, int> OnHealthChanged;
    public static event Action<HealthState> OnHealthStateChanged;
    public static event Action<int> OnDamageTaken;
    public static event Action<int> OnHealthRestored;
    public static event Action OnPlayerDeath;
    public static event Action OnPlayerRevived;
    public static event Action OnCriticalHealth;
    
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (playerCamera == null)
            playerCamera = Camera.main;
    }
    
    private void Start()
    {
        InitializeHealth();
        UpdateHealthState();
    }
    
    private void InitializeHealth()
    {
        config.currentHealth = config.maxHealth;
        config.isInvulnerable = false;
        isRegenerating = false;
        lastDamageTime = 0f;
        
        OnHealthChanged?.Invoke(config.currentHealth, config.maxHealth);
        
        if (debugMode) Debug.Log($"Player health initialized: {config.currentHealth}/{config.maxHealth}");
    }
    
    public void TakeDamage(int damage)
    {
        if (config.isInvulnerable || config.currentHealth <= 0)
        {
            if (debugMode) Debug.Log("Damage ignored - player is invulnerable or dead");
            return;
        }
        
        int actualDamage = Mathf.Min(damage, config.currentHealth);
        config.currentHealth = Mathf.Max(0, config.currentHealth - actualDamage);
        lastDamageTime = Time.time;
        
        OnHealthChanged?.Invoke(config.currentHealth, config.maxHealth);
        OnDamageTaken?.Invoke(actualDamage);
        
        PlayDamageEffects();
        UpdateHealthState();
        
        if (config.currentHealth > 0)
        {
            StartInvulnerability();
            StopRegeneration();
            StartCoroutine(StartRegenerationAfterDelay());
        }
        else
        {
            HandlePlayerDeath();
        }
        
        if (debugMode) Debug.Log($"Player took {actualDamage} damage. Health: {config.currentHealth}/{config.maxHealth}");
    }
    
    public void RestoreHealth(int amount)
    {
        if (config.currentHealth <= 0)
        {
            if (debugMode) Debug.Log("Cannot restore health - player is dead");
            return;
        }
        
        int actualHealing = Mathf.Min(amount, config.maxHealth - config.currentHealth);
        if (actualHealing <= 0) return;
        
        config.currentHealth = Mathf.Min(config.maxHealth, config.currentHealth + actualHealing);
        
        OnHealthChanged?.Invoke(config.currentHealth, config.maxHealth);
        OnHealthRestored?.Invoke(actualHealing);
        
        PlayHealingEffects();
        UpdateHealthState();
        
        if (debugMode) Debug.Log($"Player restored {actualHealing} health. Health: {config.currentHealth}/{config.maxHealth}");
    }
    
    public void SetMaxHealth(int maxHealth)
    {
        config.maxHealth = maxHealth;
        config.currentHealth = Mathf.Min(config.currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(config.currentHealth, config.maxHealth);
        UpdateHealthState();
        
        if (debugMode) Debug.Log($"Max health set to {maxHealth}. Current health: {config.currentHealth}");
    }
    
    public void FullHeal()
    {
        if (config.currentHealth <= 0)
        {
            RevivePlayer();
        }
        else
        {
            RestoreHealth(config.maxHealth);
        }
    }
    
    private void StartInvulnerability()
    {
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
        }
        
        invulnerabilityCoroutine = StartCoroutine(InvulnerabilityCoroutine());
    }
    
    private IEnumerator InvulnerabilityCoroutine()
    {
        config.isInvulnerable = true;
        
        // Visual feedback for invulnerability (flashing effect)
        if (enableScreenEffects)
        {
            StartCoroutine(FlashEffect());
        }
        
        yield return new WaitForSeconds(config.invulnerabilityDuration);
        
        config.isInvulnerable = false;
        
        if (debugMode) Debug.Log("Invulnerability period ended");
    }
    
    private IEnumerator FlashEffect()
    {
        float flashDuration = config.invulnerabilityDuration;
        float flashInterval = 0.1f;
        float elapsedTime = 0f;
        
        while (elapsedTime < flashDuration)
        {
            // Toggle alpha or color for flashing effect
            // This would need to be implemented based on your rendering setup
            
            yield return new WaitForSeconds(flashInterval);
            elapsedTime += flashInterval;
        }
    }
    
    private IEnumerator StartRegenerationAfterDelay()
    {
        yield return new WaitForSeconds(config.regenDelay);
        
        if (config.canRegenerate && config.currentHealth > 0 && config.currentHealth < config.maxHealth)
        {
            StartRegeneration();
        }
    }
    
    private void StartRegeneration()
    {
        if (regenCoroutine != null || isRegenerating)
            return;
        
        isRegenerating = true;
        regenCoroutine = StartCoroutine(RegenerationCoroutine());
        
        if (debugMode) Debug.Log("Health regeneration started");
    }
    
    private void StopRegeneration()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
            regenCoroutine = null;
        }
        
        isRegenerating = false;
        
        if (currentState == HealthState.Regenerating)
        {
            UpdateHealthState();
        }
    }
    
    private IEnumerator RegenerationCoroutine()
    {
        SetHealthState(HealthState.Regenerating);
        
        while (config.currentHealth < config.maxHealth && config.currentHealth > 0)
        {
            // Check if player has taken damage recently
            if (Time.time - lastDamageTime < config.regenDelay)
            {
                yield return new WaitForSeconds(0.5f);
                continue;
            }
            
            int regenAmount = Mathf.RoundToInt(config.regenRate);
            RestoreHealth(regenAmount);
            
            yield return new WaitForSeconds(1f);
        }
        
        isRegenerating = false;
        UpdateHealthState();
        
        if (debugMode) Debug.Log("Health regeneration completed");
    }
    
    private void HandlePlayerDeath()
    {
        SetHealthState(HealthState.Dead);
        StopRegeneration();
        
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }
        
        if (criticalStateCoroutine != null)
        {
            StopCoroutine(criticalStateCoroutine);
            criticalStateCoroutine = null;
        }
        
        PlayDeathEffects();
        OnPlayerDeath?.Invoke();
        
        if (debugMode) Debug.Log("Player has died");
    }
    
    public void RevivePlayer()
    {
        config.currentHealth = config.maxHealth;
        config.isInvulnerable = false;
        isRegenerating = false;
        lastDamageTime = 0f;
        
        OnHealthChanged?.Invoke(config.currentHealth, config.maxHealth);
        OnPlayerRevived?.Invoke();
        
        UpdateHealthState();
        
        if (debugMode) Debug.Log("Player has been revived");
    }
    
    private void UpdateHealthState()
    {
        HealthState newState = CalculateHealthState();
        
        if (newState != currentState)
        {
            SetHealthState(newState);
        }
    }
    
    private HealthState CalculateHealthState()
    {
        if (config.currentHealth <= 0)
            return HealthState.Dead;
        
        if (isRegenerating)
            return HealthState.Regenerating;
        
        float healthPercentage = (float)config.currentHealth / config.maxHealth;
        
        if (healthPercentage <= criticalHealthThreshold)
            return HealthState.Critical;
        else if (healthPercentage <= injuredHealthThreshold)
            return HealthState.Injured;
        else
            return HealthState.Healthy;
    }
    
    private void SetHealthState(HealthState newState)
    {
        currentState = newState;
        OnHealthStateChanged?.Invoke(newState);
        
        HandleStateEffects(newState);
        
        if (debugMode) Debug.Log($"Health state changed to: {newState}");
    }
    
    private void HandleStateEffects(HealthState state)
    {
        switch (state)
        {
            case HealthState.Critical:
                HandleCriticalState();
                break;
            case HealthState.Dead:
                break;
            default:
                StopCriticalEffects();
                break;
        }
    }
    
    private void HandleCriticalState()
    {
        OnCriticalHealth?.Invoke();
        
        if (criticalStateCoroutine != null)
        {
            StopCoroutine(criticalStateCoroutine);
        }
        
        criticalStateCoroutine = StartCoroutine(CriticalStateEffects());
    }
    
    private IEnumerator CriticalStateEffects()
    {
        PlaySound(criticalWarningSound);
        
        if (criticalHealthEffect != null)
        {
            GameObject effect = Instantiate(criticalHealthEffect, transform.position, Quaternion.identity);
            effect.transform.parent = transform;
        }
        
        // Play heartbeat sound periodically
        while (currentState == HealthState.Critical)
        {
            PlaySound(heartbeatSound);
            yield return new WaitForSeconds(1.5f);
        }
    }
    
    private void StopCriticalEffects()
    {
        if (criticalStateCoroutine != null)
        {
            StopCoroutine(criticalStateCoroutine);
            criticalStateCoroutine = null;
        }
        
        // Remove critical health effects
        if (criticalHealthEffect != null)
        {
            GameObject[] effects = GameObject.FindGameObjectsWithTag("CriticalHealthEffect");
            foreach (GameObject effect in effects)
            {
                Destroy(effect);
            }
        }
    }
    
    private void PlayDamageEffects()
    {
        PlaySound(damageSound);
        
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private void PlayHealingEffects()
    {
        PlaySound(healingSound);
        
        if (healingEffect != null)
        {
            GameObject effect = Instantiate(healingEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f);
        }
    }
    
    private void PlayDeathEffects()
    {
        PlaySound(deathSound);
        
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position, Quaternion.identity);
            Destroy(effect, 5f);
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public HealthConfig GetConfig()
    {
        return config;
    }
    
    public HealthState GetCurrentState()
    {
        return currentState;
    }
    
    public int GetCurrentHealth()
    {
        return config.currentHealth;
    }
    
    public int GetMaxHealth()
    {
        return config.maxHealth;
    }
    
    public float GetHealthPercentage()
    {
        return (float)config.currentHealth / config.maxHealth;
    }
    
    public bool IsAlive()
    {
        return config.currentHealth > 0;
    }
    
    public bool IsInvulnerable()
    {
        return config.isInvulnerable;
    }
    
    public bool IsRegenerating()
    {
        return isRegenerating;
    }
    
    public void SetRegenerationEnabled(bool enabled)
    {
        config.canRegenerate = enabled;
        
        if (!enabled)
        {
            StopRegeneration();
        }
    }
    
    public void SetRegenerationRate(float rate)
    {
        config.regenRate = rate;
    }
    
    private void OnDestroy()
    {
        if (regenCoroutine != null)
        {
            StopCoroutine(regenCoroutine);
        }
        
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
        }
        
        if (criticalStateCoroutine != null)
        {
            StopCoroutine(criticalStateCoroutine);
        }
    }
}