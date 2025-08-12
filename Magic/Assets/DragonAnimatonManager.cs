using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DragonAnimationManager : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform mouthSocket;

    [Header("Tags")]
    public string playerTag = "Player";

    [Header("Attack Zone (인스펙터에서 콜라이더 드래그)")]
    public Collider[] attackZoneColliders;
    public bool requireAttackZoneTag = true;
    public bool forceIsTrigger = true;
    public bool autoAddKinematicRb = true;

    [Header("Skill Prefabs")]
    public GameObject fireSkillPrefab;
    public GameObject fireeeeSkillPrefab;
    public bool attachSkillToMouth = true;
    public float fireSkillLifetime    = 1.5f;
    public float fireeeeSkillLifetime = 2.5f;
    public Vector3 skillEulerOffset   = Vector3.zero;

    [Header("External Particle Dummies (옵션)")]
    public Transform particleDummiesRoot;
    public bool clearDummiesOnStop = false;

    [Header("Timing (sec)")]
    public float crossFade         = 0.12f;
    public Vector2 idleGroundDelay = new Vector2(0.6f, 1.2f);
    public Vector2 idleAirDelay    = new Vector2(0.6f, 1.2f);
    public float takeOffLen        = 1.1f;
    public float landLen           = 1.0f;
    public float attackLenGround   = 0.9f;
    public float attackLenAir      = 1.1f;
    public float hurtLenAir        = 0.6f;
    public float dieLenDefault     = 1.0f;
    public float delayedSequenceAfter = 20f;

    // Animator state (= clip names)
    const string G_Idle     = "Idle";
    const string G_Fire     = "Fire";
    const string G_FireLong = "Fireeee";
    const string A_TakeOff  = "Fly UP";
    const string A_Idle     = "Fly Idle";
    const string A_IdleAlt  = "Flying Idle";
    const string A_Land     = "Fly Down";
    const string A_FireAir  = "Fly Fireeee";
    const string A_Hurt     = "Fly Ouch";
    const string A_DieStart = "Fly Die Start";
    const string A_DieDone  = "Fly Die Done";

    [Header("Death Freeze Options")]
    public bool freezeAnimatorOnDeath = true; // 애니메이터 프리즈
    [Range(0.9f, 0.999f)] public float freezeNormalizedTime = 0.98f;
    public bool disableThisComponentOnDeath = true; // 이 스크립트 비활성화
    public bool disableAttackZoneCollidersOnDeath = true; // 트리거 무력화

    // ---- Internal ----
    bool alive = true, inAir = false;
    bool combatStarted = false, deathScheduled = false, hasDied = false;
    Coroutine attackCo, deathCo;
    GameObject activeFX;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!mouthSocket) mouthSocket = transform;
        Play(G_Idle);
        WireAttackZones();
    }

    void OnEnable()
    {
        // 이미 죽어있다면 다시 켜져도 곧바로 비활성화
        if (hasDied)
        {
            if (freezeAnimatorOnDeath && animator)
            {
                animator.Play(A_DieDone, 0, freezeNormalizedTime);
                animator.Update(0f);
                animator.speed = 0f;
            }
            enabled = false;
        }
    }

    // 훅에서 호출
    public void OnAttackZoneEnter(Collider other)
    {
        if (!alive || combatStarted) return;
        if (!other || !other.CompareTag(playerTag)) return;
        StartCombat();
    }

    void StartCombat()
    {
        if (combatStarted || !alive) return;
        combatStarted = true;

        attackCo = StartCoroutine(AttackLoop());

        if (!deathScheduled)
        {
            deathScheduled = true;
            deathCo = StartCoroutine(DelayedFlyHurtDieSequence());
        }
    }

    IEnumerator DelayedFlyHurtDieSequence()
    {
        yield return new WaitForSeconds(Mathf.Max(0.01f, delayedSequenceAfter));

        StopActiveSkillFX();
        StopExternalDummies();

        inAir = true;
        Play(A_TakeOff);            yield return new WaitForSeconds(takeOffLen);
        Play(A_Hurt);               yield return new WaitForSeconds(hurtLenAir);

        StopActiveSkillFX();
        StopExternalDummies();

        Play(A_DieStart);           yield return new WaitForSeconds(dieLenDefault);
        Play(A_DieDone);            yield return new WaitForSeconds(dieLenDefault);

        FinalizeDeath(); // ★ 여기서 완전 종료
    }

    IEnumerator AttackLoop()
    {
        while (alive)
        {
            yield return new WaitForSeconds(Random.Range(idleGroundDelay.x, idleGroundDelay.y));

            bool doAir = Random.value < 0.5f;
            if (doAir)
            {
                inAir = true;
                Play(A_TakeOff);             yield return new WaitForSeconds(takeOffLen);
                Play(GetAirIdle());          yield return new WaitForSeconds(Random.Range(idleAirDelay.x, idleAirDelay.y));
                Play(A_FireAir);             yield return new WaitForSeconds(attackLenAir);
                Play(A_Land); inAir = false; yield return new WaitForSeconds(landLen);
            }
            else
            {
                Play(Random.value < 0.5f ? G_Fire : G_FireLong);
                yield return new WaitForSeconds(attackLenGround);
            }
        }
    }

    void FinalizeDeath()
    {
        hasDied = true;
        alive = false;

        // 코루틴 정리
        if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
        if (deathCo  != null) { StopCoroutine(deathCo);  deathCo  = null; }

        StopActiveSkillFX();
        StopExternalDummies();

        // 트리거 완전 차단
        if (disableAttackZoneCollidersOnDeath && attackZoneColliders != null)
            foreach (var c in attackZoneColliders) if (c) c.enabled = false;

        // 애니메이터 프리즈(DieDone 포즈에 고정)
        if (freezeAnimatorOnDeath && animator)
        {
            animator.Play(A_DieDone, 0, freezeNormalizedTime);
            animator.Update(0f);
            animator.speed = 0f;
        }

        // 이 스크립트 자체 비활성화 → 훅(AttackZoneHook)도 즉시 무효화됨
        if (disableThisComponentOnDeath) enabled = false;
    }

    // --- Anim / FX ---
    void Play(string state, float fade = -1f)
    {
        if (!animator || string.IsNullOrEmpty(state)) return;

        // 공격 상태가 아니면 들어가기 전에 FX 정리
        if (state != G_Fire && state != G_FireLong && state != A_FireAir)
            StopActiveSkillFX();

        // 죽은 뒤에는 어떤 상태도 새로 재생하지 않음
        if (hasDied) return;

        animator.CrossFadeInFixedTime(state, fade < 0 ? crossFade : fade);
        TrySpawnSkillForState(state);
    }

    string GetAirIdle()
    {
        if (animator && animator.HasState(0, Animator.StringToHash(A_Idle)))    return A_Idle;
        if (animator && animator.HasState(0, Animator.StringToHash(A_IdleAlt))) return A_IdleAlt;
        return A_Idle;
    }

    void TrySpawnSkillForState(string state)
    {
        if (!alive || hasDied) return;

        if (state == G_Fire)                 SpawnSkill(fireSkillPrefab,  fireSkillLifetime);
        else if (state == G_FireLong ||
                 state == A_FireAir)         SpawnSkill(fireeeeSkillPrefab, fireeeeSkillLifetime);
    }

    void SpawnSkill(GameObject prefab, float lifetime)
    {
        if (!prefab || !mouthSocket || !alive || hasDied) return;

        StopActiveSkillFX();

        var fx = Instantiate(prefab);
        if (attachSkillToMouth)
        {
            fx.transform.SetParent(mouthSocket, worldPositionStays:false);
            fx.transform.localPosition = Vector3.zero;
            fx.transform.localRotation = Quaternion.Euler(skillEulerOffset);
        }
        else
        {
            fx.transform.position = mouthSocket.position;
            fx.transform.rotation = mouthSocket.rotation * Quaternion.Euler(skillEulerOffset);
        }

        if (lifetime > 0f) Destroy(fx, lifetime);
        else activeFX = fx;
    }

    void StopActiveSkillFX()
    {
        if (!activeFX) return;
        var ps = activeFX.GetComponent<ParticleSystem>();
        if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(activeFX, 1f);
        activeFX = null;
    }

    void StopExternalDummies()
    {
        if (!particleDummiesRoot) return;
        var list = particleDummiesRoot.GetComponentsInChildren<ParticleSystem>(true);
        var behavior = clearDummiesOnStop ? ParticleSystemStopBehavior.StopEmittingAndClear
                                          : ParticleSystemStopBehavior.StopEmitting;
        foreach (var ps in list) if (ps) ps.Stop(true, behavior);
    }

    // ---- Hook wiring ----
    void WireAttackZones()
    {
        if (attackZoneColliders == null || attackZoneColliders.Length == 0)
        {
            Debug.LogWarning("[Dragon] AttackZoneColliders 비어있습니다. 인스펙터에서 트리거 콜라이더를 드래그하세요.");
            return;
        }

        foreach (var col in attackZoneColliders)
        {
            if (!col) continue;

            if (requireAttackZoneTag && !col.gameObject.CompareTag("AttackZone"))
                Debug.LogWarning($"[Dragon] '{col.gameObject.name}'는 AttackZone 태그가 아닙니다. (동작은 가능)");

            if (forceIsTrigger) col.isTrigger = true;

            if (autoAddKinematicRb)
            {
                var rb = col.GetComponent<Rigidbody>();
                if (!rb)
                {
                    rb = col.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity  = false;
                }
            }

            var hook = col.GetComponent<AttackZoneHookForManager>();
            if (!hook) hook = col.gameObject.AddComponent<AttackZoneHookForManager>();
            hook.Init(this, playerTag);
        }
    }
}

/* 숨김용 훅 — 드래곤 매니저 전용 */
[AddComponentMenu(""), DisallowMultipleComponent]
public class AttackZoneHookForManager : MonoBehaviour
{
    private DragonAnimationManager owner;
    private string playerTag;

    public void Init(DragonAnimationManager o, string tagToUse)
    {
        owner = o;
        playerTag = tagToUse;
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!owner || !owner.enabled) return;
        if (other && other.CompareTag(playerTag))
            owner.OnAttackZoneEnter(other);
    }
}
