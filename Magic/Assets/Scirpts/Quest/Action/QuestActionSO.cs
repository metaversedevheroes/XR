using UnityEngine;

public abstract class QuestActionSO : ScriptableObject
{
    public abstract void Execute(QuestStepManager stepManager);
    public abstract string GetActionDescription();
}