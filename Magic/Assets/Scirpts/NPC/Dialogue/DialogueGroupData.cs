using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueGroup", menuName = "Game/Dialogue Group")]
public class DialogueGroupAsset : ScriptableObject
{
    public string dialogueGroupID;
    public string dialogueGroupName;
    public NPCData npc;
    public QuestGroupData quest;
    public QuestStepData questStep;

    public List<DialogueData> dialogues;
}