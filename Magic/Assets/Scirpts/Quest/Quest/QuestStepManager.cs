using System;
using UnityEngine;

public class QuestStepManager 
{
    public QuestStepData stepData { get; private set; }
    public int currentCount { get; private set; }
    public bool IsStarted { get; private set; } = false;

    public QuestStepManager(QuestStepData data)
    {
        stepData = data;
        currentCount = stepData.startCount;
    }

    public void RegisterInteraction()
    {
        if (!IsStarted) IsStarted = true;

        // 행동 타입에 따른 내부 카운트 변화
        switch (stepData.actionType)
        {
            case InteractionActionType.Increase:
                currentCount++;
                break;
            case InteractionActionType.Decrease:
                currentCount--;
                break;
            case InteractionActionType.Interact:
                currentCount = 1;
                break;
        }

        // 추가 액션 실행 (SO는 이제 QuestStepManager 기준으로 실행)
        stepData.onInteractAction?.Execute(this);
    }

    public bool IsStepComplete => currentCount >= stepData.targetCount;

    // 외부에서 안전하게 카운트 조작할 수 있도록 메서드 제공
    public void AddCount(int value) => currentCount += value;
    public void SubtractCount(int value) => currentCount -= value;
}