using System.Linq;
using UnityEngine;
using Yarn.Unity;

public class DialogueRouter : MonoBehaviour
{
    public static DialogueRouter Instance { get; private set; }
    public DialogueRouteDatabase database;
    public DialogueRunner runner;

    void Awake()
    {
        Instance = this;
        runner = FindObjectOfType<DialogueRunner>();
    }

    public void StartDialogueFor(TargetData npcData)
    {
        Debug.Log($"{npcData.targetID}번의 {npcData.outterName}이름으로 정보 검색 시도 중");
        // 1) QuestSystem에게 이 NPC와 관련된 퀘스트 매니저를 요청
        var qm = QuestSystem.Instance.GetQuestForNPC(npcData.targetID);
        if (qm == null) return;

        // 2) 여기서는 계산하지 않고, QuestGroupManager에게 위임
        int  stepOrder = qm.CurrentStepOrder;
        string phase   = qm.CurrentPhase;

        Debug.Log($"{qm.groupData.title}의 {stepOrder}번쨰 퀘스트 스탭 진행중. 현재 퀘스트 스탭 진행 상태는 {phase}입니다.");
        
        // 3) Route 조회
        var route = database.routes
            .FirstOrDefault(r =>
                r.questData == qm.groupData && // 에러 발생 지점
                r.npcData   == npcData
            );
        
        if (route == null) return;
        
        Debug.Log($"{route.questData}와 {route.npcData} 정보를 활용하여, {route.yarnStartNode}의 대화 정보를 확득함");

        // 4) Yarn 변수 세팅
        runner.VariableStorage.SetValue("$questStep", stepOrder);
        runner.VariableStorage.SetValue("$stepPhase", phase);

        // 5) Yarn 실행 -> 이 부분을 이제 실제 계산된 변수를 가지고 하고 업데이트함
        runner.StartDialogue(route.yarnStartNode);
    }
}

