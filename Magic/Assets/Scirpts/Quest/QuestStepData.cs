using UnityEngine;

public enum QuestStepType { Dialogue, Collect, Quiz, Battle, Move, Collection }

[CreateAssetMenu(fileName = "QuestStep", menuName = "Game/Quest Step")]
public class QuestStepData : ScriptableObject
{
    public string step_id;
    public int step_order;
    public QuestStepType step_type;
    public string target_id; // Dialogue ID, Item Name 등
    //퀘스트를 할 떄 대상을 어떻게 잡고 있나?? 저번은 콜라이더였던 거 같은디
    public string condition;
    public string description;
}