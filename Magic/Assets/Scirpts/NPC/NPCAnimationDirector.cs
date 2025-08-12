using UnityEngine;
using System.Collections;

// NPCAnimationDirector — 드래곤 사망 연동 + 콤보/단독 VFX + 단독 1.7초 지연 최종본
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
    public string handAttackState = "Hand Attack";  // 콤보 2단(여기서 콤보 VFX 발생)
    public string handsAttackState = "Hands Attack";// 단독(여기서 단독 VFX, 지연 발생)

    [Header("Attack Settings")]
    public float attackStartDelay = 3f;
    public float attackInterval = 2f;
    public bool randomizeAttack = true;
    [Range(0f, 1f)] public float comboProbability = 0.5f;

    [Header("Animation Durations")]
    public float handUpLen = 1f;
    public float handAttackLen = 1f;
    public float handsAttackLen = 2f;

    [Header("Skill VFX (인스펙터 연결)")]
    public Transform vfxSocket;               // 손/가슴 등 기준점
    public bool attachVfxToSocket = true;     // true면 소켓에 붙어서(Local) 따라감
    public Vector3 vfxEulerOffset = Vector3.zero;

    [Tooltip("콤보(Hand Attack 시점) VFX 프리팹")]
    public GameObject comboVfxPrefab;
    public float comboVfxLifetime = 1.2f;     // 0이면 지속 → 수동정리

    [Tooltip("단독(Hands Attack 시점) VFX 프리팹")]
    public GameObject singleVfxPrefab;
    public float singleVfxLifetime = 1.2f;    // 0이면 지속 → 수동정리

    [Tooltip("단독 스킬 애니메이션 시작 후 VFX 지연(초)")]
    public float singleVfxDelay = 1.7f;       // ★ 요청 반영: 기본 1.7초

    // 상태
    private bool isWalking = false;
    private bool isTalking = false;
    private bool isInAttackZone = false;
    private bool isAttacking = false;
    private int attackCounter = 0;
    private bool dragonDefeated = false;

    // 코루틴
    private Coroutine attackCoroutine;
    private Coroutine talkCoroutine;

    // 지속형 VFX 추적
    private GameObject activeComboVfx;
    private GameObject activeSingleVfx;

    void OnEnable()
    {
        BossMonster.OnBossDefeated += HandleDragonDefeated; // 드래곤 사망 이벤트 구독
    }

    void OnDisable()
    {
        BossMonster.OnBossDefeated -= HandleDragonDefeated;
    }

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
    }

    /// <summary>드래곤이 죽었을 때 호출(이벤트로 자동 수신)</summary>
    public void HandleDragonDefeated()
    {
        if (dragonDefeated) return;
        dragonDefeated = true;

        if (attackCoroutine != null) { StopCoroutine(attackCoroutine); attackCoroutine = null; }
        if (talkCoroutine   != null) { StopCoroutine(talkCoroutine);   talkCoroutine   = null; }

        isInAttackZone = false;
        isAttacking = false;
        isTalking = false;
        isWalking = false;

        StopAllSkillVfx();
        PlayAnimation(idleState);
    }

    // === 외부 제어 API ===
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

    public void EnterAttackZone()
    {
        if (dragonDefeated) return;
        isInAttackZone = true;
        if (attackCoroutine == null)
            attackCoroutine = StartCoroutine(AttackLoop());
    }

    public void ExitAttackZone()
    {
        isInAttackZone = false;
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            attackCoroutine = null;
        }
        if (!isAttacking) UpdateMovementAnimation();
        StopAllSkillVfx();
    }

    // === 내부 로직 ===
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

    private IEnumerator AttackLoop()
    {
        // 첫 딜레이
        float waited = 0f;
        while (isInAttackZone && !dragonDefeated && waited < attackStartDelay)
        {
            waited += Time.deltaTime;
            yield return null;
        }
        if (!isInAttackZone || dragonDefeated) { attackCoroutine = null; yield break; }

        while (isInAttackZone && !dragonDefeated)
        {
            // 대화 중 대기
            while (isTalking && isInAttackZone && !dragonDefeated)
                yield return null;

            if (!isInAttackZone || dragonDefeated) break;

            yield return StartCoroutine(ExecuteAttack());

            // 쿨타임
            float t = 0f;
            while (t < attackInterval && isInAttackZone && !dragonDefeated)
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
        attackCounter++;

        bool useCombo = randomizeAttack ? (Random.value < comboProbability) : true;

        if (useCombo)
        {
            Debug.Log($"스킬 {attackCounter} — 콤보 (Hand Up → Hand Attack)");

            // 1) Hand Up
            PlayAnimation(handUpState);
            yield return new WaitForSeconds(handUpLen);

            // 2) Hand Attack + 콤보 VFX
            if (isInAttackZone && !dragonDefeated)
            {
                PlayAnimation(handAttackState);
                SpawnComboVfx(); // 콤보 VFX는 2단계에서 즉시
                yield return new WaitForSeconds(handAttackLen);
            }
        }
        else
        {
            Debug.Log($"스킬 {attackCounter} — 단독 (Hands Attack)");

            // 단독 애니 시작
            PlayAnimation(handsAttackState);

            // ★ 단독 VFX 지연: singleVfxDelay(기본 1.7초)
            float t = 0f;
            while (t < singleVfxDelay && isInAttackZone && !dragonDefeated)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // 아직 존 안이고 살아있으면 VFX 발사
            if (isInAttackZone && !dragonDefeated)
                SpawnSingleVfx();

            // 애니메이션 잔여 구간 대기
            float remain = Mathf.Max(0f, handsAttackLen - singleVfxDelay);
            if (remain > 0f)
            {
                float r = 0f;
                while (r < remain && isInAttackZone && !dragonDefeated)
                {
                    r += Time.deltaTime;
                    yield return null;
                }
            }
        }

        isAttacking = false;

        if (isInAttackZone && !dragonDefeated)
            UpdateMovementAnimation();
    }

    // === VFX 스폰/정리 ===
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
}
