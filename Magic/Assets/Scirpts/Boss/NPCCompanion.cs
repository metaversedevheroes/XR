using UnityEngine;
using System.Collections;
using System;

public class NPCCompanion : MonoBehaviour
{
    [System.Serializable]
    public class CompanionStats
    {
        public int attackDamage = 15;
        public float attackInterval = 4f;
        public float attackRange = 8f;
        public float moveSpeed = 3f;
        public bool isAttacking = false;
        public bool isActive = true;
    }
    
    public enum CompanionState
    {
        Idle,
        MovingToPosition,
        Attacking,
        Celebrating,
        Defeated
    }
    
    [Header("Companion Configuration")]
    [SerializeField] private CompanionStats stats = new CompanionStats();
    [SerializeField] private bool debugMode = true;
    [SerializeField] private float attackAnimationDuration = 1.5f;
    [SerializeField] private float celebrationDuration = 3f;
    
    [Header("Attack Patterns")]
    [SerializeField] private float swordAttackChance = 0.4f;
    [SerializeField] private float magicAttackChance = 0.3f;
#pragma warning disable CS0414
    [SerializeField] private float supportSkillChance = 0.3f; // TODO: Implement support skill probability logic
#pragma warning restore CS0414
    
    [Header("Visual Effects")]
    [SerializeField] private GameObject swordAttackEffect;
    [SerializeField] private GameObject magicAttackEffect;
    [SerializeField] private GameObject supportEffect;
    [SerializeField] private GameObject celebrationEffect;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip magicSound;
    [SerializeField] private AudioClip supportSound;
    [SerializeField] private AudioClip celebrationSound;
    
    [Header("Animation")]
    [SerializeField] private Animator animator;
    
    [Header("Movement")]
    [SerializeField] private Transform[] attackPositions;
    [SerializeField] private float positionSwitchInterval = 8f;
    
    private CompanionState currentState = CompanionState.Idle;
    private BossMonster targetBoss;
    private Transform player;
    private Coroutine attackCoroutine;
    private Coroutine movementCoroutine;
    private int currentPositionIndex = 0;
    private float lastAttackTime = 0f;
    
    public static event Action<int> OnCompanionAttack;
    public static event Action<string> OnCompanionMessage;
    public static event Action OnCompanionSupport;
    
    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    private void Start()
    {
        FindPlayer();
        SetState(CompanionState.Idle);
        
        if (attackPositions.Length == 0)
        {
            GenerateDefaultPositions();
        }
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
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                player = mainCamera.transform;
            }
        }
    }
    
    private void GenerateDefaultPositions()
    {
        attackPositions = new Transform[3];
        
        for (int i = 0; i < 3; i++)
        {
            GameObject positionObj = new GameObject($"AttackPosition_{i}");
            positionObj.transform.parent = transform.parent;
            
            float angle = (i * 120f) * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 5f;
            positionObj.transform.position = transform.position + offset;
            
            attackPositions[i] = positionObj.transform;
        }
        
        if (debugMode) Debug.Log("Generated default attack positions");
    }
    
    public void Initialize(BossMonster boss)
    {
        targetBoss = boss;
        stats.isActive = true;
        
        OnCompanionMessage?.Invoke("Your ally has joined the battle!");
        
        if (debugMode) Debug.Log("NPC Companion initialized and ready for battle");
    }
    
    public void StartAttacking()
    {
        if (stats.isActive && attackCoroutine == null)
        {
            attackCoroutine = StartCoroutine(AttackRoutine());
            movementCoroutine = StartCoroutine(MovementRoutine());
            
            if (debugMode) Debug.Log("NPC Companion started attacking");
        }
    }
    
    public void StopAttacking()
    {
        stats.isActive = false;
        
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
            movementCoroutine = null;
        }
        
        SetState(CompanionState.Idle);
        
        if (debugMode) Debug.Log("NPC Companion stopped attacking");
    }
    
    private IEnumerator AttackRoutine()
    {
        while (stats.isActive && targetBoss != null && targetBoss.IsAlive())
        {
            yield return new WaitForSeconds(stats.attackInterval);
            
            if (!stats.isAttacking && CanAttackBoss())
            {
                yield return StartCoroutine(PerformAttack());
            }
        }
    }
    
    private IEnumerator MovementRoutine()
    {
        while (stats.isActive && targetBoss != null && targetBoss.IsAlive())
        {
            yield return new WaitForSeconds(positionSwitchInterval);
            
            if (!stats.isAttacking)
            {
                yield return StartCoroutine(MoveToNextPosition());
            }
        }
    }
    
    private IEnumerator MoveToNextPosition()
    {
        if (attackPositions.Length == 0) yield break;
        
        SetState(CompanionState.MovingToPosition);
        
        currentPositionIndex = (currentPositionIndex + 1) % attackPositions.Length;
        Transform targetPosition = attackPositions[currentPositionIndex];
        
        Vector3 startPosition = transform.position;
        float journeyLength = Vector3.Distance(startPosition, targetPosition.position);
        float journeyTime = journeyLength / stats.moveSpeed;
        float elapsedTime = 0f;
        
        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;
            
            transform.position = Vector3.Lerp(startPosition, targetPosition.position, fractionOfJourney);
            
            yield return null;
        }
        
        transform.position = targetPosition.position;
        LookAtBoss();
        
        SetState(CompanionState.Idle);
        
        if (debugMode) Debug.Log($"Companion moved to position {currentPositionIndex}");
    }
    
    private IEnumerator PerformAttack()
    {
        stats.isAttacking = true;
        SetState(CompanionState.Attacking);
        
        LookAtBoss();
        
        float randomValue = UnityEngine.Random.value;
        
        if (randomValue < swordAttackChance)
        {
            yield return StartCoroutine(SwordAttack());
        }
        else if (randomValue < swordAttackChance + magicAttackChance)
        {
            yield return StartCoroutine(MagicAttack());
        }
        else
        {
            yield return StartCoroutine(SupportSkill());
        }
        
        stats.isAttacking = false;
        SetState(CompanionState.Idle);
        lastAttackTime = Time.time;
        
        if (debugMode) Debug.Log("Companion attack completed");
    }
    
    private IEnumerator SwordAttack()
    {
        PlayAttackAnimation("SwordAttack");
        PlaySound(attackSound);
        
        OnCompanionMessage?.Invoke("Companion strikes with sword!");
        
        yield return new WaitForSeconds(0.5f);
        
        if (swordAttackEffect != null && targetBoss != null)
        {
            Vector3 effectPosition = targetBoss.transform.position + Vector3.up * 2f;
            GameObject effect = Instantiate(swordAttackEffect, effectPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        int damage = stats.attackDamage;
        if (targetBoss != null)
        {
            targetBoss.TakeDamage(damage);
            OnCompanionAttack?.Invoke(damage);
        }
        
        yield return new WaitForSeconds(attackAnimationDuration - 0.5f);
        
        if (debugMode) Debug.Log($"Companion performed sword attack for {damage} damage");
    }
    
    private IEnumerator MagicAttack()
    {
        PlayAttackAnimation("MagicAttack");
        PlaySound(magicSound);
        
        OnCompanionMessage?.Invoke("Companion casts a magic spell!");
        
        yield return new WaitForSeconds(0.8f);
        
        if (magicAttackEffect != null && targetBoss != null)
        {
            Vector3 effectPosition = targetBoss.transform.position + Vector3.up * 3f;
            GameObject effect = Instantiate(magicAttackEffect, effectPosition, Quaternion.identity);
            Destroy(effect, 3f);
        }
        
        int damage = Mathf.RoundToInt(stats.attackDamage * 1.5f);
        if (targetBoss != null)
        {
            targetBoss.TakeDamage(damage);
            OnCompanionAttack?.Invoke(damage);
        }
        
        yield return new WaitForSeconds(attackAnimationDuration - 0.8f);
        
        if (debugMode) Debug.Log($"Companion performed magic attack for {damage} damage");
    }
    
    private IEnumerator SupportSkill()
    {
        PlayAttackAnimation("Support");
        PlaySound(supportSound);
        
        OnCompanionMessage?.Invoke("Companion provides support!");
        
        yield return new WaitForSeconds(0.3f);
        
        if (supportEffect != null)
        {
            Vector3 effectPosition = transform.position + Vector3.up * 2f;
            GameObject effect = Instantiate(supportEffect, effectPosition, Quaternion.identity);
            Destroy(effect, 2f);
        }
        
        OnCompanionSupport?.Invoke();
        
        yield return new WaitForSeconds(attackAnimationDuration - 0.3f);
        
        if (debugMode) Debug.Log("Companion provided support");
    }
    
    public void PlayVictoryAnimation()
    {
        StartCoroutine(VictorySequence());
    }
    
    private IEnumerator VictorySequence()
    {
        SetState(CompanionState.Celebrating);
        
        PlayCelebrationAnimation();
        PlaySound(celebrationSound);
        
        OnCompanionMessage?.Invoke("Victory! The dragon has been defeated!");
        
        if (celebrationEffect != null)
        {
            Vector3 effectPosition = transform.position + Vector3.up * 2f;
            GameObject effect = Instantiate(celebrationEffect, effectPosition, Quaternion.identity);
            Destroy(effect, celebrationDuration);
        }
        
        yield return new WaitForSeconds(celebrationDuration);
        
        SetState(CompanionState.Idle);
        
        if (debugMode) Debug.Log("Companion victory celebration completed");
    }
    
    private bool CanAttackBoss()
    {
        if (targetBoss == null || !targetBoss.IsAlive())
            return false;
        
        float distanceToBoss = Vector3.Distance(transform.position, targetBoss.transform.position);
        return distanceToBoss <= stats.attackRange;
    }
    
    private void LookAtBoss()
    {
        if (targetBoss != null)
        {
            Vector3 direction = (targetBoss.transform.position - transform.position).normalized;
            direction.y = 0;
            
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }
    
    private void SetState(CompanionState newState)
    {
        currentState = newState;
        
        if (debugMode) Debug.Log($"Companion state changed to: {newState}");
    }
    
    private void PlayAttackAnimation(string attackType)
    {
        if (animator != null)
        {
            animator.SetTrigger("Attack");
            animator.SetInteger("AttackType", attackType.GetHashCode());
        }
    }
    
    private void PlayCelebrationAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Celebrate");
        }
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    public CompanionStats GetStats()
    {
        return stats;
    }
    
    public CompanionState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsActive()
    {
        return stats.isActive;
    }
    
    public bool IsAttacking()
    {
        return stats.isAttacking;
    }
    
    public void SetAttackDamage(int damage)
    {
        stats.attackDamage = damage;
    }
    
    public void SetAttackInterval(float interval)
    {
        stats.attackInterval = interval;
    }
    
    private void Update()
    {
        if (stats.isActive && currentState == CompanionState.Idle && targetBoss != null && targetBoss.IsAlive())
        {
            LookAtBoss();
        }
    }
    
    private void OnDestroy()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        
        if (movementCoroutine != null)
        {
            StopCoroutine(movementCoroutine);
        }
    }
}