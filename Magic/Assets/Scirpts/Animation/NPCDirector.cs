// NPCDirector.cs — FINAL (Ground Snap + OnTrigger + Talk 해제 타이밍 개선 + 디버그 로그)
// - Role=NPC  : 플레이어 오른앞 추적, 바닥 스냅, 속도기반 Idle/Walk, Talk, 공격 루프
// - Role=Zone : Tag="AttackZone" 트리거에서 Player 감지 → 모든 NPC에 신호 + "AttackZone 지남!!" 로그
// - Talk 끝난 뒤엔 플레이어 계속 바라보다가 "플레이어가 움직이면" 즉시 해제 → 진행방향으로 회전 복귀
// - 스킬 시작할 때마다 "스킬 N" 로그 (콤보/단독 정보 포함)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCDirector : MonoBehaviour
{
    public enum RoleType { NPC, Zone }

    [Header("Common")]
    public RoleType role = RoleType.NPC;
    public string playerTag = "Player";
    public string attackZoneTag = "AttackZone";
    public bool   debugLogs = true;   // 요청에 따라 기본 ON

    // ================= NPC 설정 =================
    [Header("NPC: Follow (오른앞 대각선)")]
    public Vector3 localOffset = new Vector3(0.8f, 0f, 1.2f);
    public float   followSpeed = 4f;
    public float   smoothTime  = 0.15f;
    public float   rotationLerp = 10f;
    public float   stopDistance = 0.25f;

    [Header("NPC: Grounding (바닥 스냅)")]
    public bool  lockToGround = true;
    public LayerMask groundMask = ~0;
    public float groundProbeStart = 1.5f;
    public float groundProbeDistance = 4f;
    public float groundYOffset = 0f;

    [Header("NPC: Idle/Walk (속도 기준)")]
    public float moveStartSpeed = 0.20f;
    public float moveStopSpeed  = 0.10f;
    public float speedSmoothing = 10f;
    public float settleTime     = 0.12f;

    [Header("NPC: Talk (클릭 대상 1칸)")]
    public GameObject talkTarget;
    public float talkDuration = 1.5f;
    public Camera raycam;

    [Header("NPC: Talk 종료 후 시선 유지")]
    public bool keepFacingPlayerAfterTalk = true;
    public float playerMoveWakeThreshold = 0.03f; // 좀 더 민감하게

    [Header("NPC: Attack 루프")]
    public float attackStartDelay = 3f;    // 첫 공격 지연(초)
    public float attackInterval   = 5f;    // 반복 간격(초)
    public bool  randomizeAttack  = true;
    [Range(0f,1f)] public float comboProbability = 0.6f;
    public float handUpLen      = 0.7f;
    public float handAttackLen  = 0.8f;
    public float handsAttackLen = 1.0f;

    [Header("NPC: Animator States (컨트롤러와 동일)")]
    public string idleState        = "Idle";
    public string walkState        = "Walk";
    public string talkState        = "Talk";
    public string handUpState      = "Hand Up";
    public string handAttackState  = "Hand Attack";
    public string handsAttackState = "Hands Attack";
    public float  crossFade = 0.12f;

    // ================= 내부 상태(NPC) =================
    static readonly List<NPCDirector> s_npcs = new(); // 씬 내 활성 NPC 등록
    Animator anim;
    Transform target;           // Player Transform (태그로 자동)
    Vector3 lastPos, smoothVel;
    float smoothedSpeed, settleTimer;
    bool  isMoving, alive = true;

    // Talk
    string overrideKey;         // "Talk"
    float  overrideRemain;
    bool   holdFaceToPlayer;
    Vector3 lastPlayerPos;

    // Attack
    int  insideZoneCount;
    bool attacking;
    Coroutine attackCo;
    int  skillCount;            // ✅ 스킬 순번 카운터

    // ================= 생명주기 =================
    void OnEnable()
    {
        if (role == RoleType.NPC && !s_npcs.Contains(this)) s_npcs.Add(this);
    }
    void OnDisable()
    {
        if (role == RoleType.NPC) s_npcs.Remove(this);
    }

    void Awake()
    {
        if (role == RoleType.NPC)
        {
            anim = GetComponent<Animator>();

            // Rigidbody가 붙어있어도 물리와 싸우지 않도록 자동 안전 설정
            if (TryGetComponent<Rigidbody>(out var rb))
            {
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
                rb.constraints |= RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }

            ResolveTarget();
            ResolveRaycam();

            lastPos = transform.position;
            if (target) lastPlayerPos = target.position;

            Cross(idleState);
        }
        else // Zone
        {
            // Tag/Trigger 보정
            if (!CompareTag(attackZoneTag) && debugLogs)
                Debug.LogWarning($"[Zone:{name}] Tag를 '{attackZoneTag}'로 설정하세요.");
            var col = GetComponent<Collider>();
            if (!col && debugLogs) Debug.LogWarning($"[Zone:{name}] Collider가 필요합니다.");
            else if (col) col.isTrigger = true;
        }
    }

    void Update()
    {
        if (role != RoleType.NPC || !alive) return;

        if (!target) ResolveTarget();

        // ✅ 1) Talk 해제 로직을 먼저 처리 (플레이어가 움직이는 순간 바로 응시 해제되도록)
        UpdateTalkReleaseOnly();

        // ✅ 2) 속도 계산 및 Idle/Walk 결정(회전 모드 결정에 도움)
        UpdateSpeedDecisionOnly();

        // ✅ 3) 추적 이동 + 회전 + 바닥 스냅
        FollowUpdate();

        // ✅ 4) Talk 타이머/유지 처리
        UpdateTalkTimerAndHold();

        // 5) Talk 클릭 입력
        if (Input.GetMouseButtonDown(0)) TryClickTalk();
    }

    // ================= Zone: OnTrigger (태그로만 처리) =================
    void OnTriggerEnter(Collider other)
    {
        if (role != RoleType.Zone) return;
        if (!CompareTag(attackZoneTag) || !other.CompareTag(playerTag)) return;

        if (debugLogs) Debug.Log("AttackZone 지남!!");   // ✅ 요청 로그
        foreach (var npc in s_npcs) npc.ZoneEnter();
    }
    void OnTriggerExit(Collider other)
    {
        if (role != RoleType.Zone) return;
        if (!CompareTag(attackZoneTag) || !other.CompareTag(playerTag)) return;

        if (debugLogs) Debug.Log("[Zone] Player EXIT");
        foreach (var npc in s_npcs) npc.ZoneExit();
    }

    // ================= NPC: 존 신호 수신 =================
    public void ZoneEnter()
    {
        if (role != RoleType.NPC || !alive) return;
        insideZoneCount++;
        if (debugLogs) Debug.Log($"[NPC] Entered AttackZone (count={insideZoneCount})");
        TryStartAttackLoop();
    }
    public void ZoneExit()
    {
        if (role != RoleType.NPC || !alive) return;
        insideZoneCount = Mathf.Max(0, insideZoneCount - 1);
        if (debugLogs) Debug.Log($"[NPC] Exited AttackZone (count={insideZoneCount})");
        if (insideZoneCount == 0) StopAttackLoop();
    }

    void TryStartAttackLoop()
    {
        if (attacking || insideZoneCount <= 0) return;
        attacking = true;
        attackCo = StartCoroutine(AttackLoop());
    }
    void StopAttackLoop()
    {
        attacking = false;
        if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
        if (alive && string.IsNullOrEmpty(overrideKey))
            Cross(isMoving ? walkState : idleState);
    }

    IEnumerator AttackLoop()
    {
        // 첫 공격 지연
        float delay = Mathf.Max(0f, attackStartDelay);
        float waited = 0f;
        while (alive && attacking && insideZoneCount > 0 && waited < delay)
        {
            if (debugLogs) Debug.Log($"[NPC] First attack in {delay - waited:0.00}s");
            waited += Time.deltaTime;
            yield return null;
        }
        if (!alive || !attacking || insideZoneCount == 0) { attacking = false; yield break; }

        float t = 0f;
        while (alive && attacking && insideZoneCount > 0)
        {
            if (!string.IsNullOrEmpty(overrideKey)) { yield return null; continue; } // Talk 중엔 대기

            t += Time.deltaTime;
            if (t < attackInterval) { yield return null; continue; }
            t = 0f;

            // ✅ 스킬 순번 출력
            skillCount++;
            if (randomizeAttack ? (Random.value < comboProbability) : true)
            {
                if (debugLogs) Debug.Log($"스킬 {skillCount} — 콤보 시작 (Hand Up → Hand Attack)");
                Cross(handUpState);     yield return new WaitForSeconds(handUpLen);
                Cross(handAttackState); yield return new WaitForSeconds(handAttackLen);
            }
            else
            {
                if (debugLogs) Debug.Log($"스킬 {skillCount} — 단독 (Hands Attack)");
                Cross(handsAttackState); yield return new WaitForSeconds(handsAttackLen);
            }

            if (alive && string.IsNullOrEmpty(overrideKey))
                Cross(isMoving ? walkState : idleState);
        }
        attacking = false;
    }

    // ================= NPC: 기본 로직/유틸 =================
    void ResolveTarget()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        target = go ? go.transform : null;
    }
    void ResolveRaycam()
    {
        if (!raycam) raycam = Camera.main;
        if (!raycam) { var any = FindObjectOfType<Camera>(); if (any) raycam = any; }
    }

    // (1) Talk 해제 판단만 먼저 처리 — 플레이어가 움직이는 순간 즉시 해제
    void UpdateTalkReleaseOnly()
    {
        if (!keepFacingPlayerAfterTalk || !target) return;

        // Talk 중이 아니고, 현재 '응시 유지' 상태라면 플레이어 이동 감지만 체크
        if (!IsTalking() && holdFaceToPlayer)
        {
            float moved = (target.position - lastPlayerPos).magnitude;
            lastPlayerPos = target.position;
            if (moved >= playerMoveWakeThreshold)
            {
                holdFaceToPlayer = false;
                if (debugLogs) Debug.Log("[NPC] Post-Talk face hold released (player moved)");
            }
        }
    }

    // (2) 속도 계산만 — 회전모드/애니메이션 판단에 쓰임
    void UpdateSpeedDecisionOnly()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);
        float rawSpeed = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;

        float k = 1f - Mathf.Exp(-speedSmoothing * dt);
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, rawSpeed, k);

        settleTimer += Time.deltaTime;
        bool wantMove = isMoving ? (smoothedSpeed > moveStopSpeed) : (smoothedSpeed >= moveStartSpeed);

        if (settleTimer >= settleTime && string.IsNullOrEmpty(overrideKey) && wantMove != isMoving)
        {
            isMoving = wantMove;
            settleTimer = 0f;
            Cross(isMoving ? walkState : idleState);
        }
    }

    // (3) 추적 + 바닥 스냅 + 회전
    void FollowUpdate()
    {
        if (!target) return;

        Vector3 desired = target.TransformPoint(localOffset);
        if (lockToGround) desired = SnapToGround(desired);
        else desired.y = transform.position.y;

        if ((desired - transform.position).magnitude > stopDistance)
        {
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref smoothVel, smoothTime, followSpeed);
            transform.position = next;
        }
        else
        {
            smoothVel = Vector3.zero;
        }

        // 회전: Talk 중/응시 유지면 플레이어 응시, 아니면 진행방향 응시
        if (IsTalking() || holdFaceToPlayer)
        {
            if (target) FaceTarget(target, rotationLerp * 1.2f);
        }
        else
        {
            FaceDirection(Vector3.ProjectOnPlane(target.forward, Vector3.up), rotationLerp);
        }
    }

    Vector3 SnapToGround(Vector3 pos)
    {
        Vector3 origin = pos + Vector3.up * groundProbeStart;
        float dist = groundProbeStart + groundProbeDistance;
        if (Physics.Raycast(origin, Vector3.down, out var hit, dist, groundMask, QueryTriggerInteraction.Ignore))
            pos.y = hit.point.y + groundYOffset;
        return pos;
    }

    void FaceDirection(Vector3 flatForward, float lerp)
    {
        if (flatForward.sqrMagnitude < 1e-6f) return;
        var look = Quaternion.LookRotation(flatForward, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, lerp * Time.deltaTime);
    }
    void FaceTarget(Transform t, float lerp)
    {
        Vector3 dir = Vector3.ProjectOnPlane(t.position - transform.position, Vector3.up);
        if (dir.sqrMagnitude < 1e-6f) return;
        var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, lerp * Time.deltaTime);
    }

    // (4) Talk 타이머 소모 + Talk 종료 시 응시 유지 시작
    void UpdateTalkTimerAndHold()
    {
        if (overrideRemain > 0f)
        {
            overrideRemain -= Time.deltaTime;
            if (overrideRemain <= 0f)
            {
                overrideKey = null; // Talk 종료
                if (keepFacingPlayerAfterTalk && target)
                {
                    holdFaceToPlayer = true;
                    lastPlayerPos = target.position; // 기준 갱신
                    if (debugLogs) Debug.Log("[NPC] Talk End → holding face to player");
                }
            }
        }
    }

    bool IsTalking() => overrideRemain > 0f && overrideKey == "Talk";

    // Talk 입력/시작
    void TryClickTalk()
    {
        if (!raycam || !talkTarget) return;
        Ray ray = raycam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out var hit, 1000f, ~0, QueryTriggerInteraction.Collide)) return;

        var root = talkTarget.transform;
        if (hit.transform == root || hit.transform.IsChildOf(root))
            BeginTalk(talkDuration);
    }

    public void BeginTalk(float duration)
    {
        if (!alive) return;
        overrideKey = "Talk";
        overrideRemain = Mathf.Max(0.01f, duration);
        Cross(talkState);
        if (target) FaceTarget(target, rotationLerp * 2f);
    }

    public void Kill()
    {
        if (!alive) return;
        alive = false;

        StopAttackLoop();
        insideZoneCount = 0;
        overrideKey = null; overrideRemain = 0f;
        holdFaceToPlayer = false;

        Cross(idleState);
    }

    void Cross(string state, float fade = -1f)
    {
        if (role != RoleType.NPC) return;
        if (!anim || string.IsNullOrEmpty(state)) return;
        anim.CrossFadeInFixedTime(state, fade < 0f ? crossFade : fade);
    }

    void OnDrawGizmosSelected()
    {
        if (role == RoleType.NPC && target)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(target.TransformPoint(localOffset), 0.07f);
        }
    }
}
