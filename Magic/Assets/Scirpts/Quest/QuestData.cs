using UnityEngine;
using System;
using System.Collections.Generic;

public enum QuestType { Main, Side, Daily }

[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 ID")] public string quest_id;
    [Header("퀘스트 제목")] public string title;
    [Header("퀘스트 설명")] public string description;
    [Header("요구 레벨")] public int required_level;
    [Header("보상 경험치")] public int reward_xp;
    [Header("보상 아이템")] public string reward_item; // 나중에 이것도 스크럽터블 아이템으로 관리하는 게 나을 듯
    [Header("시간제한 여부")] public bool is_timed;
    [Header("시간제한 시간")] public int time_limit_sec;
    [Header("퀘스트 타입")] public QuestType quest_type;
    [Header("연결 npc")] public NPCData linked_npc;  // FK 형태 연결
    [Header("퀘스트 단계별 진행 여부")] public bool is_step_by_step;
    [Header("퀘스트 단계")] public List<QuestStepData> steps;
    
}