// DragonAnimationDirector.cs — 최종본(사망 시 브레스 완전 정지)
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

    [Header("Skill Prefabs")]
    public GameObject fireSkillPrefab;        // "Fire"
    public GameObject fireeeeSkillPrefab;     // "Fireeee", "Fly Fireeee"
    public bool attachSkillToMouth = true;    // 소켓에 붙여서 따라감(Local)
    public float fireSkillLifetime    = 1.5f; // 0이면 자동 파괴 안 함(지속)
    public float fireeeeSkillLifetime = 2.5f;
    public Vector3 skillEulerOffset   = Vector3.zero; // 방향 보정

    [Header("AttackZone 감지(존에 스크립트 불필요)")]
    public float zoneOverlapRadius   = 0.35f;
    public float zoneScanInterval    = 0.1f;
    public float delayedSequenceAfter= 20f;   // 진입 후 20초 뒤 사망 시퀀스

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

    // ===== AttackZone 감지(존에 스크립트 불필요) =====
    bool CheckPlayerEnteredAnyAttackZone()
    {
        if (!player) { ResolvePlayer(); if (!player) return false; }

        var hits = Physics.OverlapSphere(player.position, zoneOverlapRadius, ~0, QueryTriggerInteraction.Collide);
        var nowInside = new HashSet<Collider>();
        foreach (var c in hits)
        {
            if (c && IsAttackZone(c)) nowInside.Add(c);
        }

        bool entered = false;
        foreach (var z in nowInside)
            if (!insideZones.Contains(z)) { insideZones.Add(z); entered = true; }

        // exit 정리
        var remove = new List<Collider>();
        foreach (var z in insideZones)
            if (!nowInside.Contains(z)) remove.Add(z);
        foreach (var z in remove) insideZones.Remove(z);

        return entered;
    }

    bool IsAttackZone(Collider col)
    {
        Transform t = col.transform;
        while (t != null)
        {
            if (t.CompareTag(attackZoneTag)) return true;
            t = t.parent;
        }
        return false;
    }

    void ResolvePlayer()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        player = go ? go.transform : null;
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
}
