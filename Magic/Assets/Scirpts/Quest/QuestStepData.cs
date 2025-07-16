using UnityEngine;

public enum QuestStepType { Dialogue, Collect, Quiz, Battle, Move, Collection }

[CreateAssetMenu(fileName = "QuestStep", menuName = "Game/Quest Step")]
public class QuestStepData : ScriptableObject
{
    public string stepID;
    public int stepOrder;
    public QuestStepType stepType;
    public string targetID; // Dialogue ID, Item Name 등
    //퀘스트를 할 떄 대상을 어떻게 잡고 있나?? 저번은 콜라이더였던 거 같은디
    // 퀘스트 대상 
    // 횟수
    // 행동 추가 필요
    public string condition;
    public string description;
}