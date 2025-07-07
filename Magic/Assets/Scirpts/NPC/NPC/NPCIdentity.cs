using UnityEngine;

public class NPCIdentity : MonoBehaviour
{
    [Header("이 NPC의 데이터")]
    public NPCData npcData;

    void Start()
    {
        if (npcData != null)
        {
            Debug.Log($"NPC 이름: {npcData.npc_name}, 역할: {npcData.role}");
        }
    }

    public string GetNPCName()
    {
        return npcData != null ? npcData.npc_name : "알 수 없음";
    }

    public AudioClip GetVoice()
    {
        return npcData != null ? npcData.voiceClip : null;
    }
}