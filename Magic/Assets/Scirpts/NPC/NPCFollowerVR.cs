using UnityEngine;

[DisallowMultipleComponent]
public class NPCFollowerVR : MonoBehaviour
{
    [Header("VR Target Settings")]
    public string playerTag = "Player";         // XR Origin 등에 권장
    public Transform explicitTarget;            // 수동 지정 시 우선 (XR Origin 추천)
    public bool autoResolve = true;
    public float resolveInterval = 0.5f;        // 재탐색 주기(초)
    public bool useGroundPosition = true;       // 바닥 기준으로 따라가기

    [Header("VR Camera Settings")]
    public bool useCameraForDirection = true;   // VR 헤드셋 방향 사용
    public Transform vrCameraTransform;         // VR Main Camera (비우면 자동 검색)
    public bool useHeadsetYaw = true;           // 헤드셋의 Y축 회전만 사용

    [Header("Direction/Offset")]
    public Vector3 localOffset = new Vector3(0.8f, 0f, 1.2f); // (오른쪽, 위, 앞)
    public float offsetVariation = 0.2f;        // 오프셋에 약간의 변화 추가

    [Header("Movement")]
    public float followSpeed = 4f;              // 최대 추적 속도
    public float smoothTime = 0.15f;            // 스무딩
    public float stopDistance = 0.5f;           // VR에서는 조금 더 여유있게
    public float activationDistance = 15f;      // 이 거리 이상이면 추적 비활성화
    public float rotationLerp = 8f;             // VR에서는 조금 더 부드럽게
    public bool lockYToGround = true;           // Y 고정
    public float idleSpeedThreshold = 0.05f;    // 이 속도보다 작으면 Idle로 간주

    [Header("VR Optimization")]
    public bool pauseWhenPlayerStill = true;    // 플레이어가 멈추면 NPC도 멈춤
    public float playerStillThreshold = 0.1f;   // 플레이어 정지 판정 거리
    public float stillCheckTime = 1f;           // 정지 체크 시간

    [Header("Debug")]
    public bool debugLog = false;
    public bool showVRInfo = true;              // VR 관련 정보 표시

    Transform target;           // 최종 따라갈 트랜스폼(XR Origin)
    Transform vrCamera;         // VR 메인 카메라
    Vector3 velocity;           // SmoothDamp용
    Vector3 lastPos;            // 속도 계산용
    Vector3 lastPlayerPos;      // 플레이어 이전 위치
    float resolveT;
    float playerStillTime;      // 플레이어 정지 시간
    
    bool isPlayerStill;         // 플레이어 정지 상태
    Vector3 currentOffset;      // 현재 오프셋 (변화 적용된)

    Rigidbody rb;               // 있으면 kinematic 권장
    NPCAnimationDirector anim;  // 걷기/Idle 전환에 사용

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<NPCAnimationDirector>();
        
        if (rb) 
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            if (!rb.isKinematic) 
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }
        
        currentOffset = localOffset;
    }

    void Start()
    {
        ResolveTarget(force: true);
        ResolveVRCamera();
        lastPos = transform.position;
        if (target) lastPlayerPos = GetPlayerGroundPosition();
        
        if (debugLog && showVRInfo)
        {
            Debug.Log($"[NPCFollowerVR] 초기화 완료");
            Debug.Log($"Target: {(target ? target.name : "null")}");
            Debug.Log($"VR Camera: {(vrCamera ? vrCamera.name : "null")}");
        }
    }

    void Update()
    {
        // 타깃 재탐색
        if (autoResolve && (!target || !target.gameObject.activeInHierarchy))
        {
            resolveT += Time.deltaTime;
            if (resolveT >= resolveInterval)
            {
                resolveT = 0f;
                ResolveTarget();
                ResolveVRCamera();
            }
        }

        if (!target) return;

        // 플레이어 정지 상태 체크
        CheckPlayerMovement();

        // 플레이어가 너무 멀리 있으면 추적 중단
        Vector3 playerPos = GetPlayerGroundPosition();
        float distanceToPlayer = Vector3.Distance(transform.position, playerPos);
        
        if (distanceToPlayer > activationDistance)
        {
            if (anim) anim.SetWalking(false);
            return;
        }

        // 플레이어가 정지 상태이고 NPC도 충분히 가까우면 멈춤
        if (pauseWhenPlayerStill && isPlayerStill && distanceToPlayer <= stopDistance * 2f)
        {
            if (anim) anim.SetWalking(false);
            return;
        }

        // 방향 기준 결정 (VR 카메라 or 타깃)
        Transform dirRef = GetDirectionReference();
        
        // VR에서 헤드셋의 Y축 회전만 사용
        Vector3 forward, right;
        if (useHeadsetYaw && vrCamera)
        {
            Vector3 headsetForward = vrCamera.forward;
            headsetForward.y = 0f;
            headsetForward.Normalize();
            
            forward = headsetForward;
            right = Vector3.Cross(Vector3.up, forward);
        }
        else
        {
            forward = dirRef.forward;
            forward.y = 0f;
            forward.Normalize();
            
            right = dirRef.right;
            right.y = 0f;
            right.Normalize();
        }

        // 오프셋에 약간의 변화 추가 (더 자연스럽게)
        UpdateOffset();

        // 목표 위치 계산
        Vector3 desired = playerPos + right * currentOffset.x + Vector3.up * currentOffset.y + forward * currentOffset.z;

        // 멈춤 거리 체크
        Vector3 current = transform.position;
        float horizontalDistance = Vector3.Distance(
            new Vector3(current.x, 0, current.z), 
            new Vector3(desired.x, 0, desired.z)
        );
        
        if (horizontalDistance <= stopDistance)
        {
            desired = new Vector3(current.x, lockYToGround ? current.y : desired.y, current.z);
        }

        // 높이 유지
        if (lockYToGround) 
            desired.y = current.y;

        // 부드럽게 이동
        Vector3 next = Vector3.SmoothDamp(current, desired, ref velocity, smoothTime, followSpeed);
        
        if (rb && rb.isKinematic) 
            rb.MovePosition(next);
        else 
            transform.position = next;

        // 진행 방향으로 회전
        Vector3 moveDir = next - current;
        moveDir.y = 0f;
        
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerp * Time.deltaTime);
        }

        // 애니메이션 전환
        UpdateAnimation();

        lastPos = transform.position;
    }

    void CheckPlayerMovement()
    {
        if (!target) return;

        Vector3 currentPlayerPos = GetPlayerGroundPosition();
        float playerMoveDistance = Vector3.Distance(currentPlayerPos, lastPlayerPos);
        
        if (playerMoveDistance < playerStillThreshold)
        {
            playerStillTime += Time.deltaTime;
            if (playerStillTime >= stillCheckTime)
            {
                if (!isPlayerStill && debugLog)
                    Debug.Log("[NPCFollowerVR] 플레이어 정지 감지");
                isPlayerStill = true;
            }
        }
        else
        {
            if (isPlayerStill && debugLog)
                Debug.Log("[NPCFollowerVR] 플레이어 이동 재개");
            playerStillTime = 0f;
            isPlayerStill = false;
        }

        lastPlayerPos = currentPlayerPos;
    }

    void UpdateOffset()
    {
        // 시간에 따라 오프셋에 약간의 변화 추가 (더 자연스럽게)
        float time = Time.time * 0.5f;
        Vector3 variation = new Vector3(
            Mathf.Sin(time) * offsetVariation,
            0f,
            Mathf.Cos(time * 0.7f) * offsetVariation
        );
        
        currentOffset = Vector3.Lerp(currentOffset, localOffset + variation, Time.deltaTime);
    }

    void UpdateAnimation()
    {
        if (!anim) return;

        float speed = (transform.position - lastPos).magnitude / Mathf.Max(Time.deltaTime, 1e-5f);
        bool walking = speed > idleSpeedThreshold && !isPlayerStill;
        anim.SetWalking(walking);
    }

    Vector3 GetPlayerGroundPosition()
    {
        if (!target) return Vector3.zero;
        
        Vector3 pos = target.position;
        if (useGroundPosition)
            pos.y = 0f; // 바닥 기준
        return pos;
    }

    Transform GetDirectionReference()
    {
        if (useCameraForDirection && vrCamera)
            return vrCamera;
        else if (useCameraForDirection && Camera.main)
            return Camera.main.transform;
        else
            return target;
    }

    void ResolveTarget(bool force = false)
    {
        if (!force && target && target.gameObject.activeInHierarchy) return;

        // 1) 명시적 지정 우선
        if (explicitTarget && explicitTarget.gameObject.activeInHierarchy)
        {
            target = explicitTarget;
        }
        // 2) 태그로 XR Origin 찾기
        else if (!target || !target.gameObject.activeInHierarchy)
        {
            GameObject playerGO = GameObject.FindGameObjectWithTag(playerTag);
            if (playerGO) target = playerGO.transform;
        }
        // 3) XR Origin 컴포넌트로 찾기
        if (!target)
        {
            var xrOrigin = FindObjectOfType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin) target = xrOrigin.transform;
        }
        // 4) 최후의 수단: 메인 카메라
        if (!target && Camera.main)
        {
            // 메인 카메라의 부모 중에서 XR Origin 찾기
            Transform current = Camera.main.transform;
            while (current != null)
            {
                if (current.name.Contains("Origin") || current.name.Contains("XR") || current.name.Contains("Player"))
                {
                    target = current;
                    break;
                }
                current = current.parent;
            }
            
            if (!target) target = Camera.main.transform;
        }

        if (debugLog)
        {
            if (target)
                Debug.Log($"[NPCFollowerVR] Target resolved: {target.name} at {target.position}");
            else
                Debug.LogWarning("[NPCFollowerVR] Target NOT found!");
        }
    }

    void ResolveVRCamera()
    {
        // 1) 명시적 지정
        if (vrCameraTransform)
        {
            vrCamera = vrCameraTransform;
            return;
        }

        // 2) Main Camera 사용
        if (Camera.main)
        {
            vrCamera = Camera.main.transform;
            return;
        }

        // 3) Camera 태그로 찾기
        GameObject cameraGO = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraGO)
        {
            vrCamera = cameraGO.transform;
            return;
        }

        if (debugLog && showVRInfo)
            Debug.LogWarning("[NPCFollowerVR] VR Camera not found!");
    }

    void OnDrawGizmosSelected()
    {
        if (!target) return;
        
        Vector3 playerPos = GetPlayerGroundPosition();
        Transform dirRef = GetDirectionReference();
        
        Vector3 forward, right;
        if (useHeadsetYaw && vrCamera)
        {
            Vector3 headsetForward = vrCamera.forward;
            headsetForward.y = 0f;
            headsetForward.Normalize();
            forward = headsetForward;
            right = Vector3.Cross(Vector3.up, forward);
        }
        else
        {
            forward = dirRef.forward;
            forward.y = 0f;
            forward.Normalize();
            right = dirRef.right;
            right.y = 0f;
            right.Normalize();
        }
        
        Vector3 desired = playerPos + right * currentOffset.x + Vector3.up * currentOffset.y + forward * currentOffset.z;

        // 목표 위치 (하늘색)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(desired, 0.2f);
        
        // 현재 위치에서 목표 위치로의 선
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, desired);
        
        // 플레이어 바닥 위치 (초록색)
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(playerPos, Vector3.one * 0.3f);
        
        // 정지 거리 (빨간색)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(playerPos, stopDistance);
        
        // 활성화 거리 (주황색)
        Gizmos.color = Color.orange;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }

    // 런타임에서 타깃 변경용
    public void SetTarget(Transform newTarget)
    {
        explicitTarget = newTarget;
        target = newTarget;
        if (debugLog)
            Debug.Log($"[NPCFollowerVR] Target manually set to: {(newTarget ? newTarget.name : "null")}");
    }
}