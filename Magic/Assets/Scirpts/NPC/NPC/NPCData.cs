using UnityEngine;
using System;

public enum Behavior_pattern
{
    Idle, 
    Patrol,
    Walk,
    Combat, 
    Dialogue, 
    Quest,
    Event, 
    Dead, 
}

[CreateAssetMenu(fileName = "NPC", menuName = "Game/NPC")]
public class NPCData : ScriptableObject
{
    [Header("NPC ID")] public int npc_id; 
    [Header("NPC 이름")] public string npc_name;
    [Header("역할")] public string role;
    [Header("위치")] public string location;
    [Header("행동 패턴")] public Behavior_pattern behavior_pattern;
    [Header("대화 그룹 ID")] public AudioClip voiceClip;
}

