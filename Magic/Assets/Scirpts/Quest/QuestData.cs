using UnityEngine;
using System;
using System.Collections.Generic;

public enum QuestType { Main, Side, Daily }

[CreateAssetMenu(fileName = "Quest", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    [Header("퀘스트 ID")] public string questID;
    [Header("퀘스트 제목")] public string title;
    [Header("퀘스트 설명")] public string description;
    [Header("요구 레벨")] public int requiredLevel;
    [Header("보상 경험치")] public int rewardXP;
    [Header("보상 아이템")] public string rewardItem; // 나중에 이것도 스크럽터블 아이템으로 관리하는 게 나을 듯
    [Header("시간제한 여부")] public bool isTimed;
    [Header("시간제한 시간")] public int timeLimit;
    [Header("퀘스트 타입")] public QuestType questType;
    [Header("연결 npc")] public NPCData linkedNpc;  // FK 형태 연결
    [Header("퀘스트 단계별 진행 여부")] public bool isStepByStep;
    [Header("퀘스트 단계")] public List<QuestStepData> steps;
    
}