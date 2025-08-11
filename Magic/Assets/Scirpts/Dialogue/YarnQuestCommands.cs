using Yarn.Unity;
using UnityEngine;
using System.Linq;

public class YarnQuestCommands : MonoBehaviour
{
    [YarnCommand("StartQuestStep")]
    public static void StartQuestStep(string questID, string stepID)
    {
        var qs = QuestSystem.Instance;
        var gm = qs.GetQuestManager(questID);
        if (gm == null) { Debug.LogWarning($"퀘스트 없음: {questID}"); return; }

        // 스텝 시작/진행 트리거
        gm.RegisterInteraction(stepID); // 일단 첫 상호작용 = 시작으로 간주 (수정 필요)
    }

    [YarnCommand("CompleteQuestStep")]
    public static void CompleteQuestStep(string questID, string stepID)
    {
        var qs = QuestSystem.Instance;
        var gm = qs.GetQuestManager(questID);
        if (gm == null) return;

        // // 강제 완료가 필요하면 해당 스텝 매니저 currentCount를 targetCount까지 올리는 헬퍼를 추가
        var step = gm.stepManagers.FirstOrDefault(s => s.stepData.stepID == stepID);
        if (step != null)
        {
            int need = step.stepData.targetCount - step.currentCount;
            if (need > 0) step.AddCount(need);  // 안전한 조작 메서드 준비되어 있음 :contentReference[oaicite:1]{index=1}
        }
    }
}