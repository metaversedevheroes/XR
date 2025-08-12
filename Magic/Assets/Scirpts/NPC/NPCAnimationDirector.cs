using UnityEngine;
using System.Collections;

[DisallowMultipleComponent]
public class NPCAnimationDirector : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;
    public float crossFade = 0.3f;

    [Header("Animation State Names")]
    public string idleState = "Idle";
    public string walkState = "Walk";
    public string talkState = "Talk";
    public string handUpState = "Hand Up";          // 콤보 1단
    public string handAttackState = "Hand Attack";  // 콤보 2단(VFX)
    public string handsAttackState = "Hands Attack";// 단독(VFX, 지연)

    [Header("Attack Settings")]
    public float attackStartDelay = 3f;              // 공격 시작 지연
    public float attackInterval = 2f;                // 공격 간격
    public bool randomizeAttack = true;
    [Range(0f, 1f)] public float comboProbability = 0.5f;

    [Header("Animation Durations")]
    public float handUpLen = 1f;
    public float handAttackLen = 1f;
    public float handsAttackLen = 2f;

    [Header("Skill VFX (인스펙터 연결)")]
    public Transform vfxSocket;                      // 손/가슴 등 기준점
    public bool attachVfxToSocket = true;            // 소켓에 붙여(Local) 이동
    public Vector3 vfxEulerOffset = Vector3.zero;
    public GameObject comboVfxPrefab;
    public float comboVfxLifetime = 1.2f;            // 0이면 지속(수동정리)
    public GameObject singleVfxPrefab;
    public float singleVfxLifetime = 1.2f;           // 0이면 지속(수동정리)
    [Tooltip("단독 스킬 애니메이션 시작 후 VFX 지연(초)")]
    public float singleVfxDelay = 1.7f;

    [Header("Attack Zone (인스펙터에서 '콜라이더' 직접 드래그)")]
    public string playerTag = "Player";              // 플레이어 태그
    [Tooltip("AttackZone 태그가 설정된 '트리거 콜라이더'를 그대로 드래그해서 넣으세요. (여러 개 가능)")]
    public Collider[] attackZoneColliders;
    public bool requireAttackZoneTag = true;         // AttackZone 태그 권장
    public bool forceIsTrigger = true;               // 자동으로 IsTrigger 켬
    public bool autoAddKinematicRb = true;           // 트리거 이벤트 보장(kinematic RB 추가)

    [Header("Zone Robustness")]
    [Tooltip("Exit가 잠깐 들어와도 '그 시간 동안은 안 나간 걸로' 처리")]
    public float exitGraceTime = 0.5f;
    public bool debugLogs = false;

    // 상태
    private bool isWalking = false;
    private bool isTalking = false;
    private bool isAttacking = false;
    private bool dragonDefeated = false;
    private int attackCounter = 0;   // ★ 누락된 카운터 추가

    // 어택존 유지 로직
    private int zoneOverlapCount = 0;        // 중첩 카운트
    private bool isInAttackZone = false;     // 논리적 상태
    private Coroutine exitGraceCo = null;    // 그레이스 타이머

    // 코루틴
    private Coroutine attackCoroutine;
    private Coroutine talkCoroutine;

    // 지속형 VFX 추적
    private GameObject activeComboVfx;
    private GameObject activeSingleVfx;

    void OnEnable()  { BossMonster.OnBossDefeated += HandleDragonDefeated; }
    void OnDisable() { BossMonster.OnBossDefeated -= HandleDragonDefeated; }

    void Start()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!animator)
        {
            Debug.LogError("[NPCAnimationDirector] Animator component not found!");
            enabled = false;
            return;
        }
        if (!vfxSocket) vfxSocket = transform;

        PlayAnimation(idleState);
        WireAttackZones(); // 인스펙터의 트리거 콜라이더에 훅 연결
    }

    // ===== 드래곤 사망 시 NPC 리셋 =====
    public void HandleDragonDefeated()
    {
        if (dragonDefeated) return;
        dragonDefeated = true;

        StopAttackLoop();
        isTalking = isAttacking = false;
        isWalking = false;

        StopAllSkillVfx();
        PlayAnimation(idleState);
    }

    // ===== 외부 제어(걷기/말하기) =====
    public void SetWalking(bool walking)
    {
        if (dragonDefeated) return;
        isWalking = walking;
        UpdateMovementAnimation();
    }

    public void BeginTalk(float duration)
    {
        if (dragonDefeated) return;
        if (talkCoroutine != null) StopCoroutine(talkCoroutine);
        talkCoroutine = StartCoroutine(TalkSequence(duration));
    }

    // ===== AttackZone 진입/이탈(훅에서 자동 호출) =====
    public void OnAttackZoneEnter(Collider other)
    {
        if (!other || !other.CompareTag(playerTag) || dragonDefeated) return;

        zoneOverlapCount++;
        if (debugLogs) Debug.Log($"[NPC] Zone Enter by {other.name} | count = {zoneOverlapCount}");

        if (exitGraceCo != null) { StopCoroutine(exitGraceCo); exitGraceCo = null; }

        if (!isInAttackZone)
        {
            isInAttackZone = true;
            StartAttackLoopIfNeeded();
        }
    }

    public void OnAttackZoneExit(Collider other)
    {
        if (!other || !other.CompareTag(playerTag) || dragonDefeated) return;

        zoneOverlapCount = Mathf.Max(0, zoneOverlapCount - 1);
        if (debugLogs) Debug.Log($"[NPC] Zone Exit by {other.name} | count = {zoneOverlapCount}");

        if (zoneOverlapCount == 0 && exitGraceCo == null)
            exitGraceCo = StartCoroutine(ExitGraceTimer());
    }

    IEnumerator ExitGraceTimer()
    {
        if (debugLogs) Debug.Log($"[NPC] Exit grace start {exitGraceTime}s");
        yield return new WaitForSeconds(exitGraceTime);
        exitGraceCo = null;

        if (zoneOverlapCount == 0)
        {
            isInAttackZone = false;
            StopAttackLoop();
            if (!isAttacking) UpdateMovementAnimation();
            StopAllSkillVfx();
            if (debugLogs) Debug.Log("[NPC] Zone considered left after grace");
        }
    }

    void StartAttackLoopIfNeeded()
    {
        if (attackCoroutine == null && !dragonDefeated)
            attackCoroutine = StartCoroutine(AttackLoop());
    }

    void StopAttackLoop()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
    }

    // ===== 내부 로직 =====
    private void UpdateMovementAnimation()
    {
        if (isTalking || isAttacking) return;
        PlayAnimation(isWalking ? walkState : idleState);
    }

    private void PlayAnimation(string stateName)
    {
        if (!animator) return;
        animator.CrossFadeInFixedTime(stateName, crossFade);
    }

    private IEnumerator TalkSequence(float duration)
    {
        isTalking = true;
        PlayAnimation(talkState);
        yield return new WaitForSeconds(duration);
        isTalking = false;
        UpdateMovementAnimation();
        talkCoroutine = null;
    }

    private bool InZone() => isInAttackZone || zoneOverlapCount > 0 || exitGraceCo != null;

    private IEnumerator AttackLoop()
    {
        // 첫 진입 지연
        float waited = 0f;
        while (InZone() && !dragonDefeated && waited < attackStartDelay)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        if (!InZone() || dragonDefeated) { attackCoroutine = null; yield break; }

        while (InZone() && !dragonDefeated)
        {
            // 대화 중엔 대기
            while (isTalking && InZone() && !dragonDefeated)
                yield return null;

            if (!InZone() || dragonDefeated) break;

            yield return StartCoroutine(ExecuteAttack());

            // 쿨타임
            float t = 0f;
            while (t < attackInterval && InZone() && !dragonDefeated)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }
        attackCoroutine = null;
    }

    private IEnumerator ExecuteAttack()
    {
        isAttacking = true;
        int currentIndex = ++attackCounter;

        bool useCombo = randomizeAttack ? (Random.value < comboProbability) : true;

        if (useCombo)
        {
            if (debugLogs) Debug.Log($"[NPC] 스킬 {currentIndex} — 콤보 (Hand Up → Hand Attack)");

            // 1) Hand Up
            PlayAnimation(handUpState);
            float t = 0f;
            while (t < handUpLen && InZone() && !dragonDefeated)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // 2) Hand Attack + 콤보 VFX (존이 잠깐 끊겨도 그레이스 동안 이어짐)
            if (InZone() && !dragonDefeated)
            {
                PlayAnimation(handAttackState);
                SpawnComboVfx();
                t = 0f;
                while (t < handAttackLen && InZone() && !dragonDefeated)
                {
                    t += Time.deltaTime;
                    yield return null;
                }
            }
        }
        else
        {
            if (debugLogs) Debug.Log($"[NPC] 스킬 {currentIndex} — 단독 (Hands Attack)");

            // 단독 애니 시작
            PlayAnimation(handsAttackState);

            // 단독 VFX 지연 발사
            float t = 0f;
            while (t < singleVfxDelay && InZone() && !dragonDefeated)
            {
                t += Time.deltaTime;
                yield return null;
            }

            if (InZone() && !dragonDefeated)
                SpawnSingleVfx();

            // 애니 잔여 구간
            float remain = Mathf.Max(0f, handsAttackLen - singleVfxDelay);
            t = 0f;
            while (t < remain && InZone() && !dragonDefeated)
            {
                t += Time.deltaTime;
                yield return null;
            }
        }

        isAttacking = false;

        if (InZone() && !dragonDefeated)
            UpdateMovementAnimation();
    }

    // ===== VFX =====
    void SpawnComboVfx()
    {
        if (!comboVfxPrefab || !vfxSocket) return;
        if (comboVfxLifetime <= 0f) StopComboVfx();

        var go = Instantiate(comboVfxPrefab);
        if (attachVfxToSocket)
        {
            go.transform.SetParent(vfxSocket, worldPositionStays:false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(vfxEulerOffset);
        }
        else
        {
            go.transform.position = vfxSocket.position;
            go.transform.rotation = vfxSocket.rotation * Quaternion.Euler(vfxEulerOffset);
        }

        if (comboVfxLifetime > 0f) Destroy(go, comboVfxLifetime);
        else activeComboVfx = go;
    }

    void SpawnSingleVfx()
    {
        if (!singleVfxPrefab || !vfxSocket) return;
        if (singleVfxLifetime <= 0f) StopSingleVfx();

        var go = Instantiate(singleVfxPrefab);
        if (attachVfxToSocket)
        {
            go.transform.SetParent(vfxSocket, worldPositionStays:false);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.Euler(vfxEulerOffset);
        }
        else
        {
            go.transform.position = vfxSocket.position;
            go.transform.rotation = vfxSocket.rotation * Quaternion.Euler(vfxEulerOffset);
        }

        if (singleVfxLifetime > 0f) Destroy(go, singleVfxLifetime);
        else activeSingleVfx = go;
    }

    void StopComboVfx()
    {
        if (!activeComboVfx) return;
        var ps = activeComboVfx.GetComponent<ParticleSystem>();
        if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(activeComboVfx, 1f);
        activeComboVfx = null;
    }

    void StopSingleVfx()
    {
        if (!activeSingleVfx) return;
        var ps = activeSingleVfx.GetComponent<ParticleSystem>();
        if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        Destroy(activeSingleVfx, 1f);
        activeSingleVfx = null;
    }

    void StopAllSkillVfx()
    {
        StopComboVfx();
        StopSingleVfx();
    }

    // ===== AttackZone 콜라이더 훅 연결 =====
    void WireAttackZones()
    {
        if (attackZoneColliders == null || attackZoneColliders.Length == 0)
        {
            Debug.LogWarning("[NPC] AttackZoneColliders 비어있습니다. 인스펙터에서 트리거 콜라이더를 드래그하세요.");
            return;
        }

        foreach (var col in attackZoneColliders)
        {
            if (!col)
            {
                Debug.LogWarning("[NPC] AttackZoneColliders에 빈 슬롯이 있습니다.");
                continue;
            }

            if (requireAttackZoneTag && !col.gameObject.CompareTag("AttackZone"))
                Debug.LogWarning($"[NPC] '{col.gameObject.name}'는 AttackZone 태그가 아닙니다. (동작은 가능)");

            if (forceIsTrigger) col.isTrigger = true;

            if (autoAddKinematicRb)
            {
                var rb = col.GetComponent<Rigidbody>();
                if (!rb)
                {
                    rb = col.gameObject.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }

            var hook = col.GetComponent<NPCAttackZoneHook>();
            if (!hook) hook = col.gameObject.AddComponent<NPCAttackZoneHook>();
            hook.Init(this, playerTag);
        }
    }
}

/* 숨김용 훅 — 인스펙터에서 지정한 트리거에 자동 부착 */
[AddComponentMenu(""), DisallowMultipleComponent]
public class NPCAttackZoneHook : MonoBehaviour
{
    private NPCAnimationDirector owner;
    private string playerTag;

    public void Init(NPCAnimationDirector o, string tagToUse)
    {
        owner = o;
        playerTag = tagToUse;
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!owner || !owner.enabled) return;
        if (other && other.CompareTag(playerTag))
            owner.OnAttackZoneEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!owner || !owner.enabled) return;
        if (other && other.CompareTag(playerTag))
            owner.OnAttackZoneExit(other);
    }
}
