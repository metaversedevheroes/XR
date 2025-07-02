using UnityEngine;
using System;
using System.Collections.Generic;

public enum QuestType { Main, Side, Daily }

[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 ID")] public string quest_id;
    [Header("퀘스트 제목")] public string title;
    [Header("NPC ID")] public string description;
    public int required_level;
    public int reward_xp;
    public string reward_item;
    public bool is_timed;
    public int time_limit_sec;
    public QuestType quest_type;

    public NPCData linked_npc;  // FK 형태 연결
    public List<QuestStepData> steps;
}