using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueGroup", menuName = "Game/Dialogue Group")]
public class DialogueGroupAsset : ScriptableObject
{
    public string dialogueGroupID;
    public string dialogueGroupName;
    public NPCData npc;
    public QuestData quest;

    public List<DialogueData> dialogues;
}