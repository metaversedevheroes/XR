// DragonAnimationDirector.cs
// - Animator State 이름 = 애니메이션 클립 이름과 동일하게 사용
// - 트리거 콜라이더(변수로 연결)에서 Player 태그 감지 → 시퀀스 시작
// - 상황 강제/피격/사망, 지상/공중 전환, Idle 텀 포함

using System.Collections;
using UnityEngine;

public class DragonAnimationDirector : MonoBehaviour
{
    public enum Situation { IdleGround, TakeOff, IdleAir, Land, AttackGround, AttackAir, Hurt, Die }

    [Header("References")]
    public Animator animator;           // 드래곤 루트의 Animator (null이면 자동 GetComponent)
    public Collider aggroTrigger;       // 플레이어 감지용 트리거 콜라이더(자식 오브젝트 등)
    public string playerTag = "Player"; // 감지할 태그

    [Header("선택: 거리 기반 시작(트리거 대신)")]
    public bool useDistanceAggro = false;
    public Transform player;            // 거리 방식 쓸 때만 지정
    public float aggroDistance = 12f;

    // ====== Animator State 이름(클립 이름과 동일) ======
    // 지상
    const string G_Idle     = "Idle";
    const string G_Fire     = "Fire";
    const string G_FireLong = "Fireeee";
    const string G_Hurt     = "Ouch 1";

    // 공중
    const string A_TakeOff  = "Fly UP";
    const string A_Idle     = "Fly Idle";
    const string A_IdleAlt  = "Flying Idle";
    const string A_Land     = "Fly Down";
    const string A_Fire     = "Fly Fireeee";
    const string A_Hurt     = "Fly Ouch";

    // 사망(공중)
    static readonly string[] DIE_AIR = { "Fly Die Start", "Fly Die Done" };
    // 지상 사망 클립이 있다면 여기에 추가
    static readonly string[] DIE_GROUND = { /* "Die Start", "Die Done" */ };

    [Header("Timing (초) — 애니메이션 실제 길이에 맞게 조절)")]
    public float crossFade = 0.12f;
    public Vector2 idleGroundDelay = new Vector2(0.6f, 1.2f);
    public Vector2 idleAirDelay    = new Vector2(0.6f, 1.2f);
    public float takeOffLen        = 1.1f;
    public float landLen           = 1.0f;
    public float attackLenGround   = 0.9f;
    public float attackLenAir      = 1.1f;
    public float hurtLenGround     = 0.6f;
    public float hurtLenAir        = 0.6f;
    public float dieLenDefault     = 1.0f;

    [Header("Debug (수동 강제 테스트)")]
    public bool debugControl = false;
    public Situation debugSituation = Situation.IdleGround;

    bool inAir = false;
    bool alive = true;
    bool engaged = false;   // 시퀀스 시작 여부
    Coroutine flowCo;

    void Reset()
    {
        animator = GetComponent<Animator>();
    }

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();

        // 트리거 콜라이더가 지정되어 있으면, 이벤트 전달자 자동 장착
        if (aggroTrigger != null)
        {
            aggroTrigger.isTrigger = true;
            var fwd = aggroTrigger.GetComponent<DragonTriggerForwarder>();
            if (fwd == null) fwd = aggroTrigger.gameObject.AddComponent<DragonTriggerForwarder>();
            fwd.Init(this, playerTag);
        }
    }

    void Start()
    {
        Play(G_Idle); // 초기엔 지상 Idle
    }

    void Update()
    {
        if (!alive) return;

        // 수동 디버그
        if (debugControl)
        {
            ApplySituation(debugSituation);
            return;
        }

        // 선택: 거리 기반 자동 시작
        if (useDistanceAggro && !engaged && player != null)
        {
            if (Vector3.Distance(player.position, transform.position) <= aggroDistance)
                StartAggro();
        }
    }

    // === 트리거로부터 콜백 받는 함수 (Forwarder가 호출) ===
    public void OnAggroTriggerEnter(Collider other)
    {
        Debug.Log("OnAggroTriggerEnter불렸음1");
        if (!alive || engaged) return;
        StartAggro();
    }

    // === 외부/트리거에서 호출 ===
    public void StartAggro()
    {
        Debug.Log("StartAggro이번엔 얘가 보임 2");
        if (engaged || !alive) return;
        engaged = true;

        if (flowCo != null) StopCoroutine(flowCo);
        flowCo = StartCoroutine(EncounterLoop());
    }

    public void SetSituation(Situation s)
    {
        if (!alive) return;
        ApplySituation(s);
    }

    public void TakeDamage()
    {
        if (!alive) return;
        if (flowCo != null) StopCoroutine(flowCo);

        if (inAir && HasState(A_Hurt)) { Interrupt(A_Hurt); StartCoroutine(ReturnIdleAfter(hurtLenAir)); }
        else if (!inAir && HasState(G_Hurt)) { Interrupt(G_Hurt); StartCoroutine(ReturnIdleAfter(hurtLenGround)); }
        else { StartCoroutine(ReturnIdleAfter(0.5f)); }
    }

    public void Kill()
    {
        if (!alive) return;
        alive = false;
        if (flowCo != null) StopCoroutine(flowCo);
        StartCoroutine(PlayDieSequence());
    }

    // === 메인 전투 루프(예시 시나리오) ===
    IEnumerator EncounterLoop()
    {
        Debug.Log("3번출발");
        while (alive)
        {
            Debug.Log("4번시작");
            // 지상 대기
            yield return WaitSeconds(Random.Range(idleGroundDelay.x, idleGroundDelay.y));

            // 날아오르기
            Play(A_TakeOff); inAir = true;
            yield return WaitSeconds(takeOffLen);

            // 공중 대기
            Play(GetAirIdle());
            yield return WaitSeconds(Random.Range(idleAirDelay.x, idleAirDelay.y));

            // 공중 공격
            Play(A_Fire);
            yield return WaitSeconds(attackLenAir);

            // 착지
            Play(A_Land); inAir = false;
            yield return WaitSeconds(landLen);

            // 지상 대기
            Play(G_Idle);
            yield return WaitSeconds(Random.Range(idleGroundDelay.x, idleGroundDelay.y));

            // 지상 공격 (짧/긴 랜덤)
            Play(Random.value < 0.5f ? G_Fire : G_FireLong);
            yield return WaitSeconds(attackLenGround);
        }
    }

    IEnumerator ReturnIdleAfter(float t)
    {
        yield return WaitSeconds(t);
        Play(inAir ? GetAirIdle() : G_Idle);

        if (engaged && alive)
        {
            if (flowCo != null) StopCoroutine(flowCo);
            flowCo = StartCoroutine(EncounterLoop());
        }
    }

    IEnumerator PlayDieSequence()
    {
        var seq = inAir && DIE_AIR.Length > 0 ? DIE_AIR :
                  (DIE_GROUND.Length > 0 ? DIE_GROUND : DIE_AIR);

        foreach (var s in seq)
        {
            if (string.IsNullOrEmpty(s)) continue;
            Interrupt(s);
            yield return WaitSeconds(dieLenDefault);
        }
        // Dead 루프가 있으면 여기서 추가 재생
    }

    // === 상태 전환/보조 ===
    void ApplySituation(Situation s)
    {
        switch (s)
        {
            case Situation.IdleGround: inAir = false; Play(G_Idle); break;
            case Situation.TakeOff:    inAir = true;  Play(A_TakeOff); break;
            case Situation.IdleAir:    inAir = true;  Play(GetAirIdle()); break;
            case Situation.Land:       inAir = false; Play(A_Land); break;
            case Situation.AttackGround: inAir = false; Play(Random.value < 0.5f ? G_Fire : G_FireLong); break;
            case Situation.AttackAir:    inAir = true;  Play(A_Fire); break;
            case Situation.Hurt: TakeDamage(); break;
            case Situation.Die:  Kill(); break;
        }
    }

    string GetAirIdle()
    {
        if (HasState(A_Idle))    return A_Idle;
        if (HasState(A_IdleAlt)) return A_IdleAlt;
        return A_Idle; // 기본
    }

    void Play(string state, float fade = -1f)
    {
        if (string.IsNullOrEmpty(state) || animator == null) return;
        animator.CrossFadeInFixedTime(state, fade < 0 ? crossFade : fade);
    }

    void Interrupt(string state)
    {
        if (string.IsNullOrEmpty(state) || animator == null) return;
        animator.Play(state, 0, 0f);
    }

    bool HasState(string state)
    {
        if (animator == null || string.IsNullOrEmpty(state)) return false;
        return animator.HasState(0, Animator.StringToHash(state));
    }

    WaitForSeconds WaitSeconds(float t) => new WaitForSeconds(Mathf.Max(0.01f, t));
}

// ===== 트리거 이벤트 전달용 내부 컴포넌트 =====
// (aggroTrigger에 자동으로 붙여서 Player 진입을 Director에 전달)
[DisallowMultipleComponent]
public class DragonTriggerForwarder : MonoBehaviour
{
    DragonAnimationDirector director;
    string playerTag = "Player";

    public void Init(DragonAnimationDirector d, string tagToUse)
    {
        director = d;
        playerTag = tagToUse;
        var col = GetComponent<Collider>();
        if (col) col.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        //Debug.Log("자봐라잉지금용준비시작~!");
        if (director && director.enabled && other.CompareTag(playerTag))
            director.OnAggroTriggerEnter(other);
    }
}
