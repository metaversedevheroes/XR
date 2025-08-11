using UnityEngine;

/// 손목 UI를 손목 위치 기준으로 배치하면서,
/// HMD(메인 카메라) 쪽을 바라보도록 빌보드 회전/거리/크기를 자동 보정합니다.

[ExecuteAlways]
public class WristUIBillboard : MonoBehaviour
{
    [Header("필수 참조")]
    public Transform wrist;   // Left/Right Controller Transform
    public Transform hmd;     // XR Origin의 Main Camera

    [Header("손목 기준 위치 오프셋(로컬)")]
    public Vector3 localOffset = new Vector3(0.07f, 0.00f, 0.00f); // 손등 옆으로 7cm

    [Header("거리 보정")]
    [Tooltip("UI가 너무 가깝거나 멀어지지 않도록 거리 제한")]
    public float minDistance = 0.25f;     // 25cm
    public float maxDistance = 0.60f;     // 60cm
    [Tooltip("손목 기준 오프셋을 우선 사용하되, 거리 한계를 넘으면 HMD 방향으로 슬쩍 밀어냄")]
    public float distanceAdjustStrength = 0.6f; // 0~1

    [Header("회전(빌보드)")]
    [Tooltip("0=손목 방향 유지, 1=카메라 정면을 완전 응시")]
    [Range(0f, 1f)] public float billboardStrength = 0.8f;
    [Tooltip("상하 흔들림 방지: 수평면 기준으로만 회전할지 여부")]
    public bool keepUpright = true;
    [Tooltip("회전 보정 부드러움")]
    public float rotationSmooth = 16f;

    [Header("크기(시야각 기반 자동 스케일)")]
    public bool autoScaleByAngularSize = false;
    [Tooltip("UI가 차지할 목표 시야각(세로 기준, 도 단위)")]
    [Range(1f, 20f)] public float targetAngularHeightDeg = 8f;
    [Tooltip("Canvas RectTransform의 세로 물리 길이(미터 단위) – Scale=1일 때 기준 높이")]
    public float referenceHeightMeters = 0.12f; // 예: 세로 12cm UI를 기준으로 함
    [Tooltip("스케일 보정 부드러움")]
    public float scaleSmooth = 12f;

    RectTransform _rect;
    Vector3 _vel; // pos 스무딩이 필요하면 사용

    void Reset()
    {
        // 자동 참조
        var cam = Camera.main;
        if (cam) hmd = cam.transform;

        // Wrist 후보 탐색
        var t = transform.parent;
        if (t) wrist = t;
    }

    void OnValidate()
    {
        if (minDistance > maxDistance) maxDistance = minDistance;
    }

    void Awake()
    {
        _rect = GetComponent<RectTransform>();
        if (!_rect) _rect = GetComponentInChildren<RectTransform>();
    }

    void LateUpdate()
    {
        if (!wrist || !hmd) return;

        // 1) 손목 기준 기본 위치
        var desiredPos = wrist.TransformPoint(localOffset);

        // 2) 거리 보정 (너무 가깝/멀면 HMD 방향으로 보정)
        var hToUI = desiredPos - hmd.position;
        var dist  = hToUI.magnitude;
        if (dist < minDistance || dist > maxDistance)
        {
            var clamped = Mathf.Clamp(dist, minDistance, maxDistance);
            var target = hmd.position + hToUI.normalized * clamped;
            desiredPos = Vector3.Lerp(desiredPos, target, distanceAdjustStrength);
        }

        transform.position = desiredPos;

        // 3) 회전(손목 방향 ↔ 카메라 응시 빌보드) 블렌딩
        //    - 손목이 보는 방향(손등 앞)을 기준으로
        var wristForward = wrist.forward;
        var lookToCam = (hmd.position - transform.position);
        Vector3 faceDir;

        if (keepUpright)
        {
            // 수평면 기준으로만 회전
            var wf = Vector3.ProjectOnPlane(wristForward, Vector3.up).normalized;
            var cf = Vector3.ProjectOnPlane(-lookToCam, Vector3.up).normalized;
            faceDir = Vector3.Slerp(wf, cf, billboardStrength).normalized;
            var targetRot = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
        }
        else
        {
            var cf = (-lookToCam).normalized;
            faceDir = Vector3.Slerp(wristForward.normalized, cf, billboardStrength).normalized;
            var targetRot = Quaternion.LookRotation(faceDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotationSmooth * Time.deltaTime));
        }

        // 4) 시야각 기반 자동 스케일(세로 기준)
        if (autoScaleByAngularSize && _rect)
        {
            var d = Vector3.Distance(hmd.position, transform.position);
            // 원하는 세로 물리 길이 H = 2 * d * tan(theta/2)
            var thetaRad = targetAngularHeightDeg * Mathf.Deg2Rad;
            var desiredHeight = 2f * d * Mathf.Tan(thetaRad * 0.5f);

            if (referenceHeightMeters > 0.0001f)
            {
                var current = transform.localScale;
                var targetUniform = desiredHeight / referenceHeightMeters;
                var s = Mathf.Lerp(current.x, targetUniform, 1f - Mathf.Exp(-scaleSmooth * Time.deltaTime));
                transform.localScale = new Vector3(s, s, s);
            }
        }
    }
}
