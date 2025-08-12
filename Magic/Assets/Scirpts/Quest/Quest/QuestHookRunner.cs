using UnityEngine;

// 나중엔 자동으로 걸리게 해야할 듯 ?
[RequireComponent(typeof(TargetIdentity))]
public class QuestHookRunner : MonoBehaviour {
    public QuestHookSO[] hooks; // 일단은 배치로

    TargetIdentity _id;
    void Awake(){ _id = GetComponent<TargetIdentity>(); }

    // 공통 진입점
    void Fire(QuestEventType evt) {
        if (hooks == null) return;

        string targetID = _id?.targetData?.targetID;
        foreach (var h in hooks) {
            if (h == null || h.when != evt) continue;

            var finalTargetID = string.IsNullOrEmpty(h.targetIDOverride) ? targetID : h.targetIDOverride;
            if (string.IsNullOrEmpty(finalTargetID)) continue;

            // 시스템에 "신호"만 보낸다. 수치 증감/완료 이동은 내부 매니저/스텝의 ActionSO가 수행.
            QuestSystem.Instance.ReportProgress(finalTargetID, h.stepType); // ← 단일 입구
        }
    }

    // 아래 메서드들은 타이밍별로 외부에서/자기 자신에서 호출
    public void Notify(QuestEventType evt = QuestEventType.OnInteract) => Fire(evt);
}