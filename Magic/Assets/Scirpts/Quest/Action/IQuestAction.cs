using UnityEngine;

public interface IQuestAction
{
    void Execute(QuestStepManager stepManager); // 실행 방식
    string GetActionDescription();    // 설명용 텍스트
}
