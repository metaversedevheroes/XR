using UnityEngine;

/// 카메라 정면 일정 거리에서 빌보드처럼 유지하는 HUD
[ExecuteAlways]
public class HeadHUD : MonoBehaviour
{
    [Header("필수")]
    public Transform hmd;                  // Main Camera

    [Header("배치")]
    public float distance = 0.9f;          // 0.7~1.0m 권장
    public Vector3 localOffset = new(0f, -0.10f, 0f); // 눈높이보다 살짝 아래
    public bool keepUpright = true;        // 수평 유지
    public float moveSmooth = 14f;
    public float rotSmooth  = 18f;

    [Header("자동 크기(선택)")]
    public bool autoScale = true;
    [Range(2f, 14f)] public float targetAngularHeightDeg = 8f;
    public float referenceHeightMeters = 0.12f; // 캔버스 세로 실제 길이(Scale=1 기준)
    public float scaleSmooth = 12f;
    public Vector2 scaleClamp = new(0.5f, 1.2f);

    void Reset() { if (!hmd && Camera.main) hmd = Camera.main.transform; }

    void LateUpdate()
    {
        if (!hmd) return;

        // 위치: 카메라 정면 distance + 약간의 오프셋
        var targetPos = hmd.position + hmd.forward * distance + hmd.TransformVector(localOffset);
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-moveSmooth * Time.deltaTime));

        // 회전: 정면 빌보드
        var fwd = hmd.forward;
        if (keepUpright) fwd = Vector3.ProjectOnPlane(fwd, Vector3.up).normalized;
        var targetRot = Quaternion.LookRotation(fwd, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotSmooth * Time.deltaTime));

        // 자동 스케일(시야각 유지)
        if (autoScale && referenceHeightMeters > 1e-4f)
        {
            var d = Vector3.Distance(hmd.position, transform.position);
            var desiredH = 2f * d * Mathf.Tan(targetAngularHeightDeg * Mathf.Deg2Rad * 0.5f);
            var s = Mathf.Clamp(desiredH / referenceHeightMeters, scaleClamp.x, scaleClamp.y);
            var cur = transform.localScale.x;
            var lerp = Mathf.Lerp(cur, s, 1f - Mathf.Exp(-scaleSmooth * Time.deltaTime));
            transform.localScale = Vector3.one * lerp;
        }
    }
}
