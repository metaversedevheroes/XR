using UnityEngine;

[CreateAssetMenu(fileName = "IncrementAction_", menuName = "Game/Quest/Action/Increment")]
public class IncrementActionSO : QuestActionSO
{
    public int amount = 0;

    public override void Execute(QuestStepManager stepManager)
    {
        stepManager.AddCount(amount);
        Debug.Log($"[퀘스트] {stepManager.stepData.stepID} - {amount} 증가 (현재: {stepManager.currentCount})");
    }

    public override string GetActionDescription()
    {
        return $"+{amount} 증가";
    }
}