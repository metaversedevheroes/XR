using System.Collections.Generic;
using System.Linq;

public class QuestGroupManager
{
    public QuestGroupData groupData { get; private set; }
    public List<QuestStepManager> stepManagers = new();
    private int currentStepIndex = 0;

    public QuestGroupManager(QuestGroupData data)
    {
        groupData = data;

        foreach (var step in data.steps)
            stepManagers.Add(new QuestStepManager(step));
    }

    public void RegisterInteraction(string stepID)
    {
        var step = stepManagers.FirstOrDefault(s => s.stepData.stepID == stepID);
        if (step == null || step.IsStepComplete) return;

        // 순차 진행일 경우, 현재 스텝이 아니면 무시
        if (groupData.isStepByStep && step != stepManagers[currentStepIndex]) return;

        step.RegisterInteraction();

        if (step.IsStepComplete && currentStepIndex < stepManagers.Count - 1)
            currentStepIndex++;
    }

    public bool IsQuestCompleted => stepManagers.All(s => s.IsStepComplete);
}