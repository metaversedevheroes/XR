using UnityEngine;
using System.Collections;

public class NPCAnimationDirector : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    public float crossFade = 0.3f;
    
    [Header("Animation State Names")]
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string talkState = "Talk";
    public string handUpState = "Hand Up";
    public string handAttackState = "Hand Attack";
    public string handsAttackState = "Hands Attack";
    
    [Header("Attack Settings")]
    public float attackStartDelay = 3f;
    public float attackInterval = 2f;
    public bool randomizeAttack = true;
    [Range(0f, 1f)]
    public float comboProbability = 0.5f;
    
    [Header("Animation Durations")]
    public float handUpLen = 1f;
    public float handAttackLen = 1f;
    public float handsAttackLen = 2f;
    
    // Private variables
    private bool isWalking = false;
    private bool isTalking = false;
    private bool isInAttackZone = false;
    private bool isAttacking = false;
    private int attackCounter = 0;
    
    // Coroutines
    private Coroutine attackCoroutine;
    private Coroutine talkCoroutine;
    
    void Start()
    {
        // Get animator if not assigned
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        
        if (animator == null)
        {
            Debug.LogError("Animator component not found!");
            return;
        }
        
        // Start with idle animation
        PlayAnimation(idleState);
    }
    
    /// <summary>
    /// Set walking state from NPCFollower
    /// </summary>
    public void SetWalking(bool walking)
    {
        isWalking = walking;
        UpdateMovementAnimation();
    }
    
    /// <summary>
    /// Begin talk animation
    /// </summary>
    public void BeginTalk(float duration)
    {
        if (talkCoroutine != null)
        {
            StopCoroutine(talkCoroutine);
        }
        
        talkCoroutine = StartCoroutine(TalkSequence(duration));
    }
    
    /// <summary>
    /// Enter attack zone - start attack loop
    /// </summary>
    public void EnterAttackZone()
    {
        isInAttackZone = true;
        
        if (attackCoroutine == null)
        {
            attackCoroutine = StartCoroutine(AttackLoop());
        }
    }
    
    /// <summary>
    /// Exit attack zone - stop attack loop
    /// </summary>
    public void ExitAttackZone()
    {
        isInAttackZone = false;
        
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        
        // Return to movement animation if not attacking
        if (!isAttacking)
        {
            UpdateMovementAnimation();
        }
    }
    
    /// <summary>
    /// Update movement animation (Idle/Walk) based on current state
    /// </summary>
    private void UpdateMovementAnimation()
    {
        if (isTalking || isAttacking) return;
        
        if (isWalking)
        {
            PlayAnimation(walkState);
        }
        else
        {
            PlayAnimation(idleState);
        }
    }
    
    /// <summary>
    /// Play animation using CrossFadeInFixedTime
    /// </summary>
    private void PlayAnimation(string stateName)
    {
        if (animator == null) return;
        
        animator.CrossFadeInFixedTime(stateName, crossFade);
    }
    
    /// <summary>
    /// Talk sequence coroutine
    /// </summary>
    private IEnumerator TalkSequence(float duration)
    {
        isTalking = true;
        PlayAnimation(talkState);
        
        yield return new WaitForSeconds(duration);
        
        isTalking = false;
        
        // Return to appropriate animation
        UpdateMovementAnimation();
        
        talkCoroutine = null;
    }
    
    /// <summary>
    /// Attack loop coroutine
    /// </summary>
    private IEnumerator AttackLoop()
    {
        // Initial delay before first attack
        yield return new WaitForSeconds(attackStartDelay);
        
        while (isInAttackZone)
        {
            // Wait if talking
            while (isTalking && isInAttackZone)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            if (!isInAttackZone) break;
            
            // Execute attack
            yield return StartCoroutine(ExecuteAttack());
            
            // Wait for next attack
            if (isInAttackZone)
            {
                yield return new WaitForSeconds(attackInterval);
            }
        }
        
        attackCoroutine = null;
    }
    
    /// <summary>
    /// Execute a single attack (combo or single)
    /// </summary>
    private IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        attackCounter++;
        
        bool useCombo = randomizeAttack ? (Random.value < comboProbability) : true;
        
        if (useCombo)
        {
            // Combo attack: Hand Up -> Hand Attack
            Debug.Log($"스킬 {attackCounter} — 콤보 (Hand Up → Hand Attack)");
            
            // Play Hand Up
            PlayAnimation(handUpState);
            yield return new WaitForSeconds(handUpLen);
            
            // Play Hand Attack
            if (isInAttackZone) // Check if still in zone
            {
                PlayAnimation(handAttackState);
                yield return new WaitForSeconds(handAttackLen);
            }
        }
        else
        {
            // Single attack: Hands Attack
            Debug.Log($"스킬 {attackCounter} — 단독 (Hands Attack)");
            
            PlayAnimation(handsAttackState);
            yield return new WaitForSeconds(handsAttackLen);
        }
        
        isAttacking = false;
        
        // Return to movement animation
        if (isInAttackZone)
        {
            UpdateMovementAnimation();
        }
    }
}