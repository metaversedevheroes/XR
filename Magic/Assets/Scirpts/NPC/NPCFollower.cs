// NPCFollower.cs (Anchor + Ground Snap 지원)
using UnityEngine;
using UnityEngine.AI;

public class NPCFollower : MonoBehaviour
{
    [Header("Target")]
    public Transform target;              // 기본 타깃(없으면 동작X)
    [Tooltip("타깃의 특정 지점을 기준으로 따라가고 싶으면 여기에 지정(예: Player/Anchor)")]
    public Transform anchor;              // 있으면 anchor 기준, 없으면 target 기준

    [Tooltip("+X=오른쪽, +Z=앞. 앵커(또는 타깃) 로컬 기준 오프셋")]
    public Vector3 localOffset = new Vector3(0.7f, 0f, 1.2f);

    [Header("Separation")]
    public float minSeparationFromTarget = 0.9f;
    public float stopDistance = 0.25f;

    [Header("Move (non-NavMesh)")]
    public float followSpeed = 4f;
    public float smoothTime = 0.15f;
    public float rotationLerp = 10f;

    [Header("Ground Snap (옵션)")]
    public bool snapToGround = true;
    public float groundRayStart = 1.5f;
    public float groundRayLength = 5f;
    public LayerMask groundMask = ~0;     // 기본: 전부(필요하면 Ground만 지정)

    NavMeshAgent agent;
    Vector3 smoothVel;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.updateRotation = false;
            agent.stoppingDistance = stopDistance;
        }
    }

    Transform Origin => anchor != null ? anchor : target;

    void OnValidate()
    {
        if (stopDistance >= minSeparationFromTarget)
            stopDistance = Mathf.Max(0.05f, minSeparationFromTarget * 0.7f);
    }

    void Update()
    {
        if (!Origin) return;

        // 1) 기준(Origin = anchor or target) 로컬 오프셋 → 월드
        Vector3 desiredPos = Origin.TransformPoint(localOffset);

        // 2) 바닥 스냅(선택)
        if (snapToGround)
        {
            Vector3 rayStart = desiredPos + Vector3.up * groundRayStart;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayLength, groundMask, QueryTriggerInteraction.Ignore))
                desiredPos.y = hit.point.y;
            else
                desiredPos.y = Origin.position.y; // 실패 시 원점 높이
        }
        else
        {
            desiredPos.y = Origin.position.y;
        }

        // 3) 최소거리 보장(겹침 방지)
        Vector3 away = desiredPos - Origin.position; away.y = 0f;
        float dist = away.magnitude;
        if (dist > 1e-4f && dist < minSeparationFromTarget)
            desiredPos = Origin.position + away.normalized * minSeparationFromTarget;

        Vector3 toMe = transform.position - Origin.position; toMe.y = 0f;
        float cur = toMe.magnitude;
        if (cur > 1e-4f && cur < minSeparationFromTarget)
            desiredPos += toMe.normalized * (minSeparationFromTarget - cur) * 0.6f;

        // 4) 이동
        if (agent != null && agent.isOnNavMesh)
        {
            if (agent.destination != desiredPos) agent.SetDestination(desiredPos);
        }
        else
        {
            Vector3 to = desiredPos - transform.position;
            if (to.magnitude > stopDistance)
                transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref smoothVel, smoothTime, followSpeed);
        }

        // 5) 회전: 타깃 전방을 향하게
        Quaternion look = Quaternion.LookRotation(Vector3.ProjectOnPlane(Origin.forward, Vector3.up), Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, rotationLerp * Time.deltaTime);
    }

    public void SetTarget(Transform newTarget, Transform newAnchor = null)
    {
        target = newTarget;
        anchor = newAnchor;
        if (agent != null && agent.isOnNavMesh && target != null)
        {
            agent.ResetPath();
            Vector3 p = (anchor ? anchor : target).TransformPoint(localOffset);
            agent.SetDestination(p);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Origin) return;
        Gizmos.color = Color.cyan;
        Vector3 p = Origin.TransformPoint(localOffset);
        Gizmos.DrawSphere(p, 0.07f);
        Gizmos.DrawLine(transform.position, p);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(Origin.position, minSeparationFromTarget);
    }
}
