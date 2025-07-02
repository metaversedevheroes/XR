using UnityEngine;

public enum QuestStepType { Dialogue, Collect, Quiz, Battle, Move, Collection }

[CreateAssetMenu(fileName = "QuestStep", menuName = "Game/Quest Step")]
public class QuestStepData : ScriptableObject
{
    public string step_id;
    public int step_order;
    public QuestStepType step_type;

    public string target_id; // Dialogue ID, Item Name 등
    public string condition;
    public string description;
}