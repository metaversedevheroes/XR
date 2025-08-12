// DragonAnimationDirector.cs — 최종본(AttackZone 감지 수정)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DragonAnimationDirector : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    [Tooltip("입 앞쪽 Empty(자식). 로컬 Z가 바깥을 향하게")]
    public Transform mouthSocket;

    [Header("Tags")]
    public string playerTag = "Player";
    public string attackZoneTag = "AttackZone";
    
    [Header("VR Player Settings")]
    [Tooltip("VR의 경우 XR Origin이나 카메라 오프셋 오브젝트를 직접 지정")]
    public Transform vrPlayerTransform;
    [Tooltip("플레이어 위치를 바닥 기준으로 계산할지 (Y축 0으로)")]
    public bool useGroundPosition = true;

    [Header("Skill Prefabs")]
    public GameObject fireSkillPrefab;        // "Fire"
    public GameObject fireeeeSkillPrefab;     // "Fireeee", "Fly Fireeee"
    public bool attachSkillToMouth = true;    // 소켓에 붙여서 따라감(Local)
    public float fireSkillLifetime    = 1.5f; // 0이면 자동 파괴 안 함(지속)
    public float fireeeeSkillLifetime = 2.5f;
    public Vector3 skillEulerOffset   = Vector3.zero; // 방향 보정

    [Header("AttackZone 감지")]
    public float zoneOverlapRadius   = 1.0f;  // 0.35f에서 1.0f로 증가
    public float zoneScanInterval    = 0.1f;
    public float delayedSequenceAfter= 20f;   // 진입 후 20초 뒤 사망 시퀀스
    public bool debugZoneDetection   = true;  // 디버깅용

    [Header("Animator States(클립명과 동일)")]
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

    [Header("Timing (sec)")]
    public float crossFade       = 0.12f;
    public Vector2 idleGroundDelay = new Vector2(0.6f, 1.2f);
    public Vector2 idleAirDelay    = new Vector2(0.6f, 1.2f);
    public float takeOffLen      = 1.1f;
    public float landLen         = 1.0f;
    public float attackLenGround = 0.9f;
    public float attackLenAir    = 1.1f;
    public float hurtLenAir      = 0.6f;
    public float dieLenDefault   = 1.0f;

    // --- 내부 상태 ---
    Transform player;
    bool alive = true;
    bool inAir = false;

    float scanT;
    readonly HashSet<Collider> insideZones = new();
    bool combatStarted = false;
    bool deathScheduled = false;
    Coroutine attackCo, deathCo;

    // 현재 살아있는 스킬 FX 추적(지속형 강제 종료용)
    GameObject activeFX;

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        if (!mouthSocket) mouthSocket = transform;
        ResolvePlayer();
        Play(G_Idle);
        
        if (debugZoneDetection)
            Debug.Log($"[Dragon] 초기화 완료. Player: {(player ? player.name : "null")}, ZoneRadius: {zoneOverlapRadius}");
    }

    void Update()
    {
        if (!alive || combatStarted) return;

        scanT += Time.deltaTime;
        if (scanT >= zoneScanInterval)
        {
            scanT = 0f;
            if (CheckPlayerEnteredAnyAttackZone())
                StartCombat();
        }
    }

    // ===== Combat Start & Death Schedule =====
    void StartCombat()
    {
        if (combatStarted || !alive) return;
        combatStarted = true;

        if (debugZoneDetection)
            Debug.Log("[Dragon] 전투 시작!");

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

        // 사망 시퀀스 진입 전 모든 브레스/스킬 FX 강제 종료
        StopActiveSkillFX();

        // 공중으로
        inAir = true;
        Play(A_TakeOff);
        yield return WaitForSeconds(takeOffLen);

        // 피격
        Play(A_Hurt);
        yield return WaitForSeconds(hurtLenAir);

        // 사망
        StopActiveSkillFX();
        Play(A_DieStart);
        yield return WaitForSeconds(dieLenDefault);

        Play(A_DieDone);
        yield return WaitForSeconds(dieLenDefault);

        // 완전 종료 처리
        alive = false;
        StopActiveSkillFX();
        if (attackCo != null) { StopCoroutine(attackCo); attackCo = null; }
    }

    // ===== 공격 루프(단순 예시) =====
    IEnumerator AttackLoop()
    {
        while (alive)
        {
            yield return WaitForSeconds(Random.Range(idleGroundDelay.x, idleGroundDelay.y));

            bool doAir = Random.value < 0.5f;

            if (doAir)
            {
                inAir = true;
                Play(A_TakeOff);             yield return WaitForSeconds(takeOffLen);
                Play(GetAirIdle());          yield return WaitForSeconds(Random.Range(idleAirDelay.x, idleAirDelay.y));
                Play(A_FireAir);             yield return WaitForSeconds(attackLenAir);
                Play(A_Land); inAir = false; yield return WaitForSeconds(landLen);
            }
            else
            {
                Play(Random.value < 0.5f ? G_Fire : G_FireLong);
                yield return WaitForSeconds(attackLenGround);
            }
        }
    }

    // ===== AttackZone 감지 개선 =====
    bool CheckPlayerEnteredAnyAttackZone()
    {
        if (!player) 
        { 
            ResolvePlayer(); 
            if (!player) 
            {
                if (debugZoneDetection)
                    Debug.LogWarning("[Dragon] 플레이어를 찾을 수 없습니다.");
                return false; 
            }
        }

        // VR의 경우 바닥 기준 위치 계산
        Vector3 checkPosition = player.position;
        if (useGroundPosition)
        {
            checkPosition.y = 0f; // 바닥 기준으로 계산
        }

        if (debugZoneDetection)
            Debug.Log($"[Dragon] 플레이어 원본 위치: {player.position}, 감지 위치: {checkPosition}, 감지 반경: {zoneOverlapRadius}");

        // 모든 Collider 검색 (Trigger와 일반 Collider 모두 포함)
        var hits = Physics.OverlapSphere(checkPosition, zoneOverlapRadius, -1, QueryTriggerInteraction.Collide);
        var nowInside = new HashSet<Collider>();
        
        if (debugZoneDetection)
            Debug.Log($"[Dragon] 플레이어 주변에서 {hits.Length}개 콜라이더 감지");

        foreach (var hit in hits)
        {
            if (hit)
            {
                if (debugZoneDetection)
                    Debug.Log($"[Dragon] 감지된 콜라이더: {hit.name}, 태그: {hit.tag}, IsTrigger: {hit.isTrigger}");
                
                if (IsAttackZone(hit)) 
                {
                    nowInside.Add(hit);
                    if (debugZoneDetection)
                        Debug.Log($"[Dragon] ★ AttackZone 발견: {hit.name}");
                }
            }
        }

        // 새로 진입한 존 확인
        bool entered = false;
        foreach (var zone in nowInside)
        {
            if (!insideZones.Contains(zone)) 
            { 
                insideZones.Add(zone); 
                entered = true;
                if (debugZoneDetection)
                    Debug.Log($"[Dragon] ★★ AttackZone 진입 확인: {zone.name}");
            }
        }

        // 나간 존들 정리
        var toRemove = new List<Collider>();
        foreach (var zone in insideZones)
        {
            if (!nowInside.Contains(zone)) 
            {
                toRemove.Add(zone);
                if (debugZoneDetection)
                    Debug.Log($"[Dragon] AttackZone 이탈: {zone.name}");
            }
        }
        foreach (var zone in toRemove) 
            insideZones.Remove(zone);

        return entered;
    }

    bool IsAttackZone(Collider col)
    {
        if (!col) return false;

        if (debugZoneDetection)
            Debug.Log($"[Dragon] 태그 체크 중: {col.name}, 태그: '{col.tag}', 찾는 태그: '{attackZoneTag}'");

        // 직접 태그 확인
        if (col.CompareTag(attackZoneTag))
        {
            if (debugZoneDetection)
                Debug.Log($"[Dragon] ★ {col.name}이 AttackZone 태그를 가지고 있음!");
            return true;
        }

        // 부모 오브젝트들도 확인
        Transform current = col.transform.parent;
        int parentLevel = 1;
        while (current != null && parentLevel <= 5) // 최대 5단계까지만 확인
        {
            if (debugZoneDetection)
                Debug.Log($"[Dragon] 부모 {parentLevel}단계 체크: {current.name}, 태그: '{current.tag}'");
                
            if (current.CompareTag(attackZoneTag))
            {
                if (debugZoneDetection)
                    Debug.Log($"[Dragon] ★ {current.name} (부모 {parentLevel}단계)이 AttackZone 태그를 가지고 있음!");
                return true;
            }
            current = current.parent;
            parentLevel++;
        }

        return false;
    }

    void ResolvePlayer()
    {
        // VR Transform이 직접 지정되어 있다면 그것을 사용
        if (vrPlayerTransform != null)
        {
            player = vrPlayerTransform;
            if (debugZoneDetection)
                Debug.Log($"[Dragon] VR 플레이어 Transform 사용: {player.name}");
            return;
        }

        // 태그로 찾기
        var go = GameObject.FindGameObjectWithTag(playerTag);
        player = go ? go.transform : null;
        
        if (debugZoneDetection)
        {
            if (player)
            {
                Debug.Log($"[Dragon] 태그로 플레이어 찾음: {player.name}");
                
                // VR 관련 컴포넌트들 찾기 시도
                var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
                if (xrOrigin != null)
                {
                    Debug.Log($"[Dragon] XR Origin 발견: {xrOrigin.name} at {xrOrigin.transform.position}");
                }
                
                var camera = Camera.main;
                if (camera != null)
                {
                    Debug.Log($"[Dragon] Main Camera 위치: {camera.transform.position}");
                }
            }
            else
                Debug.LogWarning($"[Dragon] '{playerTag}' 태그를 가진 플레이어를 찾을 수 없습니다!");
        }
    }

    // ===== 상태 재생 & 스킬 스폰 =====
    void Play(string state, float fade = -1f)
    {
        if (string.IsNullOrEmpty(state) || !animator) return;

        // 불 공격 상태가 아니면, 들어가기 전에 항상 FX 정리
        if (state != G_Fire && state != G_FireLong && state != A_FireAir)
            StopActiveSkillFX();

        animator.CrossFadeInFixedTime(state, fade < 0f ? crossFade : fade);

        if (!alive) return; // 사망 중엔 스폰 금지
        TrySpawnSkillForState(state);
    }

    string GetAirIdle()
    {
        if (HasState(A_Idle))    return A_Idle;
        if (HasState(A_IdleAlt)) return A_IdleAlt;
        return A_Idle;
    }

    void TrySpawnSkillForState(string state)
    {
        if (!alive) return;

        if (state == G_Fire)
        {
            SpawnSkill(fireSkillPrefab, fireSkillLifetime);
        }
        else if (state == G_FireLong || state == A_FireAir)
        {
            SpawnSkill(fireeeeSkillPrefab, fireeeeSkillLifetime);
        }
    }

    void SpawnSkill(GameObject prefab, float lifetime)
    {
        if (!prefab || !mouthSocket || !alive) return;

        // 기존 지속형 FX가 있다면 먼저 정리
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

        if (lifetime > 0f)                // 일회성
        {
            Destroy(fx, lifetime);
        }
        else                               // 지속형 → 추적해 두었다가 강제 종료
        {
            activeFX = fx;
        }
    }

    void StopActiveSkillFX()
    {
        if (!activeFX) return;

        // 파티클이면 부드럽게 멈춤
        var ps = activeFX.GetComponent<ParticleSystem>();
        if (ps) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        Destroy(activeFX, 1f);
        activeFX = null;
    }

    // ===== 유틸 =====
    bool HasState(string s)
    {
        if (!animator || string.IsNullOrEmpty(s)) return false;
        return animator.HasState(0, Animator.StringToHash(s));
    }
    WaitForSeconds WaitForSeconds(float t) => new WaitForSeconds(Mathf.Max(0.01f, t));

    void OnDisable()  { StopActiveSkillFX(); }
    void OnDestroy()  { StopActiveSkillFX(); }

    // ===== 디버깅용 기즈모 =====
    void OnDrawGizmosSelected()
    {
        if (!player) return;

        // VR의 경우 바닥 기준 위치 계산
        Vector3 checkPosition = player.position;
        if (useGroundPosition)
        {
            checkPosition.y = 0f;
        }

        // 플레이어 주변 감지 범위 표시
        Gizmos.color = combatStarted ? Color.red : Color.yellow;
        Gizmos.DrawWireSphere(checkPosition, zoneOverlapRadius);

        // 원본 플레이어 위치도 표시 (파란색)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(player.position, Vector3.one * 0.2f);

        // 현재 감지된 AttackZone들 표시
        Gizmos.color = Color.green;
        foreach (var zone in insideZones)
        {
            if (zone)
                Gizmos.DrawWireCube(zone.bounds.center, zone.bounds.size);
        }
    }
}