// NPCDirector.cs  — ONE SCRIPT, ONE CLASS (NPC/Zone 겸용 + OnTrigger)
// - 같은 스크립트를 NPC와 AttackZone 양쪽에 붙여서 Role만 바꿔 사용
// - Role=NPC : 플레이어 오른앞 추적, 속도기반 Idle/Walk, Talk 클릭, 공격 루프
// - Role=Zone: Trigger에서 Player 태그 감지(OnTriggerEnter/Exit) → NPC에 공격 시작/중단 신호
// - 공격 시작 지연: attackStartDelay(기본 3초, 인스펙터 조절)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NPCDirector : MonoBehaviour
{
    public enum RoleType { NPC, Zone }
    [Header("Common")]
    public RoleType role = RoleType.NPC;

    [Header("Tags")]
    public string playerTag = "Player";
    public string attackZoneTag = "AttackZone"; // Zone 오브젝트에 이 태그가 있어야 함

    // ====================== NPC 모드 설정 ======================
    [Header("NPC: Follow (오른앞 대각선)")]
    public Vector3 localOffset = new Vector3(0.8f, 0f, 1.2f);
    public float followSpeed = 4f;
    public float smoothTime = 0.15f;
    public float rotationLerp = 10f;
    public float stopDistance = 0.25f;

    [Header("NPC: Idle/Walk 전환 (속도 기준)")]
    public float moveStartSpeed = 0.20f;    // m/s 이상 Walk
    public float moveStopSpeed  = 0.10f;    // m/s 이하 Idle
    public float speedSmoothing = 10f;
    public float settleTime     = 0.12f;

    [Header("NPC: Talk (클릭 대상 1칸)")]
    public GameObject talkTarget;           // 루트(자식 콜라이더 허용)
    public float talkDuration = 1.5f;
    public Camera raycam;

    [Header("NPC: Talk 종료 후 시선 유지")]
    public bool keepFacingPlayerAfterTalk = true;
    public float playerMoveWakeThreshold = 0.05f;

    [Header("NPC: Attack 루프")]
    public float attackStartDelay = 3f;     // ✅ 첫 공격 지연(초) — 인스펙터에서 조절
    public float attackInterval   = 5f;     // 공격 간격(초)
    public bool  randomizeAttack  = true;
    [Range(0f,1f)] public float comboProbability = 0.6f; // 콤보 확률
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
    public float crossFade = 0.12f;

    // ====================== Zone 모드 설정 ======================
    [Header("Zone: 통지 설정")]
    [Tooltip("true면 씬의 모든 NPC에 알림, false면 반경 내 가장 가까운 NPC 한 명만")]
    public bool notifyAllNPCs = true;
    public float notifyRadius = 100f;

    [Header("Debug")]
    public bool debugLogs = false;

    // --------- 내부 상태(NPC) ---------
    static readonly List<NPCDirector> s_npcs = new(); // 등록된 NPC들
    Transform target;                 // Player Transform(자동)
    Animator anim;
    Vector3 lastPos, smoothVel;
    float smoothedSpeed, settleTimer;
    bool isMoving, alive = true;

    string overrideKey;               // "Talk"
    float  overrideRemain;
    bool   holdFaceToPlayer;
    Vector3 lastPlayerPos;

    // Attack 진행
    int insideZoneCount;              // Zone 신호로 관리
    bool attacking;                   // 공격 루프 돌고 있는지
    Coroutine attackCo;

    void OnEnable()
    {
        if (role == RoleType.NPC) { if (!s_npcs.Contains(this)) s_npcs.Add(this); }
    }
    void OnDisable()
    {
        if (role == RoleType.NPC) { s_npcs.Remove(this); }
    }

    void Awake()
    {
        if (role == RoleType.NPC)
        {
            anim = GetComponent<Animator>();
            ResolveTarget();
            ResolveRaycam();

            lastPos = transform.position;
            if (target) lastPlayerPos = target.position;

            Cross(idleState);
        }
        else // Zone 모드
        {
            // 안전장치: 태그/트리거 보정
            if (!CompareTag(attackZoneTag))
            {
                if (debugLogs) Debug.LogWarning($"[NPCDirector-Zone] '{name}' 에 Tag '{attackZoneTag}' 가 필요합니다.");
            }
            var col = GetComponent<Collider>();
            if (!col)
            {
                if (debugLogs) Debug.LogWarning($"[NPCDirector-Zone] '{name}' 에 Collider가 필요합니다.");
            }
            else
            {
                col.isTrigger = true; // 반드시 Trigger
            }
        }
    }

    void Update()
    {
        if (role != RoleType.NPC || !alive) return;

        if (!target) ResolveTarget();

        // 1) 추적
        FollowUpdate();

        // 2) Talk 유지/해제 + Talk 종료 후 응시 유지
        UpdateTalkAndFacing();

        // 3) 속도 기반 Idle/Walk
        UpdateSpeedAndAnimate();

        // 4) Talk 클릭
        if (Input.GetMouseButtonDown(0)) TryClickTalk();
    }

    // ====================== Zone 모드: OnTrigger ======================
    void OnTriggerEnter(Collider other)
    {
        if (role != RoleType.Zone) return;
        if (!other.CompareTag(playerTag)) return;

        if (debugLogs) Debug.Log($"[Zone:{name}] Player ENTER");

        // 알림 대상 NPC 선택
        if (notifyAllNPCs)
        {
            foreach (var npc in s_npcs) npc.ZoneEnterFrom(this);
        }
        else
        {
            var nearest = s_npcs
                .Where(n => n && n.isActiveAndEnabled)
                .OrderBy(n => (n.transform.position - transform.position).sqrMagnitude)
                .FirstOrDefault();
            if (nearest) nearest.ZoneEnterFrom(this);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (role != RoleType.Zone) return;
        if (!other.CompareTag(playerTag)) return;

        if (debugLogs) Debug.Log($"[Zone:{name}] Player EXIT");

        if (notifyAllNPCs)
        {
            foreach (var npc in s_npcs) npc.ZoneExitFrom(this);
        }
        else
        {
            var nearest = s_npcs
                .Where(n => n && n.isActiveAndEnabled)
                .OrderBy(n => (n.transform.position - transform.position).sqrMagnitude)
                .FirstOrDefault();
            if (nearest) nearest.ZoneExitFrom(this);
        }
    }

    // ====================== NPC 모드: Zone 신호 수신 ======================
    public void ZoneEnterFrom(NPCDirector zone)
    {
        if (role != RoleType.NPC || !alive) return;
        insideZoneCount++;
        if (debugLogs) Debug.Log($"[NPC] Entered AttackZone (count={insideZoneCount})");
        TryStartAttackLoop();
    }
    public void ZoneExitFrom(NPCDirector zone)
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

            // 공격 수행
            if (randomizeAttack ? (Random.value < comboProbability) : true)
            {
                Cross(handUpState);     if (debugLogs) Debug.Log("[NPC] Attack: Hand Up");
                yield return new WaitForSeconds(handUpLen);

                Cross(handAttackState); if (debugLogs) Debug.Log("[NPC] Attack: Hand Attack");
                yield return new WaitForSeconds(handAttackLen);
            }
            else
            {
                Cross(handsAttackState); if (debugLogs) Debug.Log("[NPC] Attack: Hands Attack");
                yield return new WaitForSeconds(handsAttackLen);
            }

            if (alive && string.IsNullOrEmpty(overrideKey))
                Cross(isMoving ? walkState : idleState);
        }
        attacking = false;
    }

    // ====================== NPC 모드: 기본 로직 ======================
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

    void FollowUpdate()
    {
        if (!target) return;

        Vector3 desired = target.TransformPoint(localOffset);
        desired.y = target.position.y;

        if ((desired - transform.position).magnitude > stopDistance)
        {
            Vector3 next = Vector3.SmoothDamp(transform.position, desired, ref smoothVel, smoothTime, followSpeed);
            transform.position = next;
        }
        else
        {
            smoothVel = Vector3.zero;
        }

        // 회전
        if ((overrideKey == "Talk") || holdFaceToPlayer)
        {
            if (target) FaceTarget(target, rotationLerp * 1.2f);
        }
        else
        {
            FaceDirection(Vector3.ProjectOnPlane(target.forward, Vector3.up), rotationLerp);
        }
    }

    void FaceDirection(Vector3 flatForward, float lerp)
    {
        if (flatForward.sqrMagnitude < 1e-6f) return;
        var look = Quaternion.LookRotation(flatForward, VectorBox.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, lerp * Time.deltaTime);
    }
    void FaceTarget(Transform t, float lerp)
    {
        Vector3 dir = Vector3.ProjectOnPlane(t.position - transform.position, Vector3.up);
        if (dir.sqrMagnitude < 1e-6f) return;
        var look = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, lerp * Time.deltaTime);
    }

    void UpdateTalkAndFacing()
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
                    lastPlayerPos = target.position;
                }
            }
        }
        if (holdFaceToPlayer && target)
        {
            float moved = (target.position - lastPlayerPos).magnitude;
            lastPlayerPos = target.position;
            if (moved >= playerMoveWakeThreshold)
                holdFaceToPlayer = false;
        }
    }

    void UpdateSpeedAndAnimate()
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
}
