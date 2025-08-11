using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueGroup", menuName = "Game/Dialogue Group")]
public class DialogueGroupAsset : ScriptableObject
{
    public string dialogue_group_id;
    public string dialogue_group_name;
    public NPCData npc;
    public QuestData quest;

    public List<DialogueData> dialogues;
}