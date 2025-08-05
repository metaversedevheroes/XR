using UnityEngine;
using System.Collections;
using System;

public class BossMonster : MonoBehaviour
{
    [System.Serializable]
    public class BossStats
    {
        public int maxHealth = 500;
        public int currentHealth = 500;
        public float attackCooldown = 2f;
        public float attackRange = 10f;
        public float moveSpeed = 2f;
        public bool isAlive = true;
        public bool isAttacking = false;
    }
    
    public enum BossState
    {
        Idle,
        Roaring,
        Preparing,
        Attacking,
        TakingDamage,
        Defeated
    }
    
    [Header("Boss Configuration")]
    [SerializeField] private BossStats stats = new BossStats();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float roarDuration = 3f;
    [SerializeField] private float attackAnimationDuration = 2f;
    [SerializeField] private float damageFlashDuration = 0.5f;
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject fireBreathEffect;
    [SerializeField] private GameObject clawAttackEffect;
    [SerializeField] private GameObject damageEffect;
    [SerializeField] private GameObject deathEffect;
    [SerializeField] private Renderer bossRenderer;
    [SerializeField] private Color normalColor = Color.red;
    [SerializeField] private Color damageColor = Color.white;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip roarSound;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip damageSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    private BossState currentState = BossState.Idle;
    private Transform player;
    private Coroutine currentAttackCoroutine;
    private Coroutine damageFlashCoroutine;
    
    public static event Action<int, int> OnBossHealthChanged;
    public static event Action OnBossAttackStarted;
    public static event Action OnBossAttackFinished;
    public static event Action OnBossDefeated;
    
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
        
        if (bossRenderer == null)
            bossRenderer = GetComponentInChildren<Renderer>();
    }
    
    private void Start()
    {
        FindPlayer();
        SetState(BossState.Idle);
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            if (debugMode) Debug.LogWarning("Player not found! Looking for MainCamera instead.");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                player = mainCamera.transform;
            }
        }
    }
    
    public void Initialize(int maxHealth)
    {
        stats.maxHealth = maxHealth;
        stats.currentHealth = maxHealth;
        stats.isAlive = true;
        stats.isAttacking = false;
        
        OnBossHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
        
        StartCoroutine(IntroductionSequence());
        
        if (debugMode) Debug.Log($"Boss initialized with {maxHealth} health");
    }
    
    private IEnumerator IntroductionSequence()
    {
        SetState(BossState.Roaring);
        
        PlayRoarAnimation();
        PlaySound(roarSound);
        
        yield return new WaitForSeconds(roarDuration);
        
        SetState(BossState.Idle);
        
        if (debugMode) Debug.Log("Boss introduction completed");
    }
    
    public void TakeDamage(int damage)
    {
        if (!stats.isAlive || stats.currentHealth <= 0)
            return;
        
        stats.currentHealth = Mathf.Max(0, stats.currentHealth - damage);
        OnBossHealthChanged?.Invoke(stats.currentHealth, stats.maxHealth);
        
        if (damageFlashCoroutine != null)
            StopCoroutine(damageFlashCoroutine);
        damageFlashCoroutine = StartCoroutine(DamageFlash());
        
        PlayDamageAnimation();
        PlaySound(damageSound);
        
        if (damageEffect != null)
        {
            GameObject effect = Instantiate(damageEffect, transform.position + Vector3.up * 2f, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        if (stats.currentHealth <= 0)
        {
            HandleDefeat();
        }
        else
        {
            StartCoroutine(DamageReaction());
        }
        
        if (debugMode) Debug.Log($"Boss took {damage} damage. Health: {stats.currentHealth}/{stats.maxHealth}");
    }
    
    private IEnumerator DamageReaction()
    {
        SetState(BossState.TakingDamage);
        yield return new WaitForSeconds(0.5f);
        
        if (stats.isAlive)
        {
            SetState(BossState.Idle);
        }
    }
    
    private IEnumerator DamageFlash()
    {
        if (bossRenderer != null)
        {
            Color originalColor = bossRenderer.material.color;
            bossRenderer.material.color = damageColor;
            
            yield return new WaitForSeconds(damageFlashDuration);
            
            bossRenderer.material.color = originalColor;
        }
    }
    
    public void PerformAttack()
    {
        if (!stats.isAlive || stats.isAttacking)
            return;
        
        if (currentAttackCoroutine != null)
            StopCoroutine(currentAttackCoroutine);
        
        currentAttackCoroutine = StartCoroutine(AttackSequence());
    }
    
    private IEnumerator AttackSequence()
    {
        stats.isAttacking = true;
        SetState(BossState.Preparing);
        OnBossAttackStarted?.Invoke();
        
        LookAtPlayer();
        
        yield return new WaitForSeconds(0.5f);
        
        SetState(BossState.Attacking);
        
        int attackType = Random.Range(0, 2);
        
        switch (attackType)
        {
            case 0:
                yield return StartCoroutine(FireBreathAttack());
                break;
            case 1:
                yield return StartCoroutine(ClawAttack());
                break;
        }
        
        yield return new WaitForSeconds(stats.attackCooldown);
        
        stats.isAttacking = false;
        SetState(BossState.Idle);
        OnBossAttackFinished?.Invoke();
        
        if (debugMode) Debug.Log("Boss attack sequence completed");
    }
    
    private IEnumerator FireBreathAttack()
    {
        PlayAttackAnimation("FireBreath");
        PlaySound(attackSound);
        
        yield return new WaitForSeconds(0.5f);
        
        if (fireBreathEffect != null)
        {
            Vector3 effectPosition = transform.position + transform.forward * 3f + Vector3.up;
            GameObject effect = Instantiate(fireBreathEffect, effectPosition, transform.rotation);
            Destroy(effect, 3f);
        }
        
        yield return new WaitForSeconds(attackAnimationDuration - 0.5f);
        
        if (debugMode) Debug.Log("Boss performed fire breath attack");
    }
    
    private IEnumerator ClawAttack()
    {
        PlayAttackAnimation("ClawAttack");
        PlaySound(attackSound);
        
        yield return new WaitForSeconds(0.3f);
        
        if (clawAttackEffect != null)
        {
            Vector3 effectPosition = transform.position + transform.forward * 2f + Vector3.up;
            GameObject effect = Instantiate(clawAttackEffect, effectPosition, transform.rotation);
            Destroy(effect, 1f);
        }
        
        yield return new WaitForSeconds(attackAnimationDuration - 0.3f);
        
        if (debugMode) Debug.Log("Boss performed claw attack");
    }
    
    private void LookAtPlayer()
    {
        if (player != null && stats.isAlive)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 2f);
            }
        }
    }
    
    private void HandleDefeat()
    {
        stats.isAlive = false;
        stats.isAttacking = false;
        
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
            currentAttackCoroutine = null;
        }
        
        SetState(BossState.Defeated);
        OnBossDefeated?.Invoke();
        
        StartCoroutine(DeathSequence());
        
        if (debugMode) Debug.Log("Boss defeated!");
    }
    
    private IEnumerator DeathSequence()
    {
        PlayDeathAnimation();
        PlaySound(deathSound);
        
        if (deathEffect != null)
        {
            GameObject effect = Instantiate(deathEffect, transform.position + Vector3.up, Quaternion.identity);
            Destroy(effect, 5f);
        }
        
        yield return new WaitForSeconds(3f);
        
        // Fade out or play death animation
        if (bossRenderer != null)
        {
            Color currentColor = bossRenderer.material.color;
            float fadeTime = 2f;
            float elapsedTime = 0f;
            
            while (elapsedTime < fadeTime)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeTime);
                currentColor.a = alpha;
                bossRenderer.material.color = currentColor;
                yield return null;
            }
        }
        
        if (debugMode) Debug.Log("Boss death sequence completed");
    }
    
    public void PlayDefeatedAnimation()
    {
        if (!stats.isAlive)
        {
            PlayDeathAnimation();
        }
    }
    
    private void SetState(BossState newState)
    {
        currentState = newState;
        
        if (debugMode) Debug.Log($"Boss state changed to: {newState}");
    }
    
    private void PlayRoarAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Roar");
        }
    }
    
    private void PlayAttackAnimation(string attackType)
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetString("AttackType", attackType);
        }
    }
    
    private void PlayDamageAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
    }
    
    private void PlayDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public BossStats GetStats()
    {
        return stats;
    }
    
    public BossState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsAlive()
    {
        return stats.isAlive;
    }
    
    public bool IsAttacking()
    {
        return stats.isAttacking;
    }
    
    public float GetHealthPercentage()
    {
        return (float)stats.currentHealth / stats.maxHealth;
    }
    
    private void Update()
    {
        if (stats.isAlive && currentState == BossState.Idle)
        {
            LookAtPlayer();
        }
    }
    
    private void OnDestroy()
    {
        if (currentAttackCoroutine != null)
        {
            StopCoroutine(currentAttackCoroutine);
        }
        
        if (damageFlashCoroutine != null)
        {
            StopCoroutine(damageFlashCoroutine);
        }
    }
}