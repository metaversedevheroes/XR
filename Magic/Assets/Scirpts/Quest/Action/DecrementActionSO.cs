using UnityEngine;

[CreateAssetMenu(fileName = "DecrementAction_", menuName = "Game/Quest/Action/Decrement")]
public class DecrementActionSO : QuestActionSO
{
    public int amount = 0;

    public override void Execute(QuestStepManager stepManager)
    {
        stepManager.AddCount(amount);
        Debug.Log($"[퀘스트] {stepManager.stepData.stepID} - {amount} 감소 (현재: {stepManager.currentCount})");
    }

    public override string GetActionDescription()
    {
        return $"+{amount} 감소";
    }
}
