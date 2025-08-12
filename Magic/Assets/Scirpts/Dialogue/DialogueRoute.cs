using UnityEngine;

[CreateAssetMenu(menuName="Game/Dialogue/Route",  fileName="DialogueRoute_")]
public class DialogueRoute : ScriptableObject {
    public QuestGroupData questData;            // 퀘스트 식별자
    public TargetData npcData;              // NPC 식별자 (TargetData.targetID)
    public string yarnStartNode;      // 이 조합에 맞는 Yarn 노드 이름
}

// public enum QuestDialogueState
// {
//     // 아직 한번도 퀘스트 제안이 이루어지지 않은 초기 상태, 제안을 할 수 없는 상태(사전 조건 미완) 
//     NotStarted,
//
//     // NPC가 퀘스트 수락을 제안하는 단계, 제안을 할 수 있는 상태(사전 조건 완료 등)
//     Offer,
//
//     // 플레이어가 퀘스트를 수락했지만, 본격적으로 행동을 시작하기 전 준비 단계
//     Accepted,
//
//     // 스텝1~N까지 **진행 중**인 단계
//     InProgress,
//     
//     // 퀘스트 목표를 달성했으며, NPC에게 돌아와서 보상을 받고 턴인해야 하는 단계
//     ReadyToTurnIn,
//
//     // 턴인(보상 수령) 처리가 완료된 순간의 대화 단계
//     TurnedIn,
//
//     // 퀘스트가 완전 완료된 이후, 반복적으로 나오는 후속 대화 단계
//     Completed,
//
//     // 퀘스트를 실패했을 때 나올 대화 단계 (타임아웃, 목표 미달성 등)
//     Failed
// }
