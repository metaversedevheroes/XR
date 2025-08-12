using UnityEngine;

public enum QuestEventType { OnInteract, OnDialogueEnded, OnDeath, OnCollected, OnEnteredRange }

[CreateAssetMenu(menuName="Game/Quest/Hook")]
public class QuestHookSO : ScriptableObject {
    [Header("언제 보낼까?")]
    public QuestEventType when;

    [Header("무엇을 갱신할까?")]
    public QuestStepType stepType;        // Dialogue/Collect/Battle/Move // 그냥 어떤 행동하는 지 함고용
    public string targetIDOverride;       // 비워두면 현재 TargetIdentity 사용
    public bool onlyIfActive = true;      // 활성 퀘스트/현재 스텝만 허용 이거 step by step 이랑 같은 기능, 정리 필요 

}
