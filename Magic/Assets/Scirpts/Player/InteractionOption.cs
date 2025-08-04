using UnityEngine;

public abstract class InteractionOption: ScriptableObject // 동일 상호작용 재사용을 위한 코드
{
    public string actionID;
    public string actionName;
    public string displayText;      
    public KeyCode key;             
    public abstract void Execute(GameObject interactor, GameObject target); // 실행하다
    // interactor = 상호작용을 시도한 객체(일반적으로 플레이어), target - 상호작용 대상 객체(즉 NPC, 퀘스트, 오브젝트 등등)
    // 누가, 누구에게 어떤 행동을 했는지 기반으로 실행
}