using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 수정 필요
public class QuestGroupManager  
{
    public QuestGroupData groupData { get; private set; }
    public List<QuestStepManager> stepManagers = new();
    public int currentStepIndex { get; private set; }

    // 퀘스트 완료 시 호출할 이벤트
    public event System.Action OnQuestCompleted;

    public QuestGroupManager(QuestGroupData data)
    {
        groupData = data;
        stepManagers = data.steps
            .Select(s => new QuestStepManager(s))
            .ToList();
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
        
        if (IsQuestCompleted)
            OnQuestCompleted?.Invoke();
    }
    
    
    // 현재 진행 중인 스텝 데이터
    public QuestStepManager CurrentStepManager =>
        stepManagers.FirstOrDefault(s => !s.IsStepComplete);

    // 현재 스텝 번호 (1-based). 완료된 경우 마지막 스텝 번호 반환
    public int CurrentStepOrder
    {
        get
        {
            var c = CurrentStepManager;
            return c != null 
                ? c.stepData.stepOrder 
                : groupData.steps.Count;
        }
    }

    // 현재 단계 페이즈 Pre/InProgress/Post 혹은 Quest 전체 레벨 Pre/InProgress/Post
    // 여기서 분기를 어떻게 나눴는지 다시 확인
    public string CurrentPhase
    {
        get
        {
            var c = CurrentStepManager;
            if (c == null)
                return IsQuestCompleted ? "Post" : "Pre";
            if (!c.IsStarted)     return "Pre";
            if (!c.IsStepComplete) return "InProgress";
            return "Post";
        }
    }

    public bool IsQuestCompleted => stepManagers.All(s => s.IsStepComplete);
    
    // 대상/종류로 스텝을 찾아서 알림
    public bool TryTriggerBy(string targetID, QuestStepType stepType)
    {
        // 순차 진행이면 현재 스텝만 허용
        QuestStepManager candidate = groupData.isStepByStep
            ? stepManagers[currentStepIndex]
            : stepManagers.FirstOrDefault(sm => !sm.IsStepComplete);

        // 매칭: 대상 + 스텝 타입
        if (candidate == null ||
            candidate.stepData.target == null ||
            candidate.stepData.target.targetID != targetID ||
            candidate.stepData.stepType != stepType)
            return false;

        candidate.TriggerActionFromOutside();

        // 다음 스텝으로 진행/완료 처리
        if (candidate.IsStepComplete && currentStepIndex < stepManagers.Count - 1)
            currentStepIndex++;

        if (IsQuestCompleted)
            OnQuestCompleted?.Invoke();

        return true;
    }

}