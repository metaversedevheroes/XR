using UnityEngine;

public enum QuestStepType { Dialogue, Collect, Quiz, Battle, Move, Collection }
public enum InteractionActionType { None, Interact, Increase, Decrease }

[CreateAssetMenu(fileName = "QuestStep", menuName = "Game/Quest Step")]
public class QuestStepData : ScriptableObject
{
    [Header("기본 정보")]
    public string stepID;
    public int stepOrder;
    public QuestStepType stepType;

    [Header("목표 대상")]
    public TargetData target;

    [Header("행동 설정")]
    public InteractionActionType actionType;
    public QuestActionSO onInteractAction;

    [Header("수치 설정")]
    public int targetCount;    // 완료 조건
    public int startCount;     // 시작 수치

    [Header("설명")]
    [TextArea]
    public string description;
}