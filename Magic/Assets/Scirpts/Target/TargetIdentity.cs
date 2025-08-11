using UnityEngine;

public class TargetIdentity : MonoBehaviour
{
    [Header("이 target의 데이터")]
    public TargetData targetData;
    
    void Start()
    {
        if (targetData != null)
        {
            // var h = GetComponent<Health>();
            // if (h != null)
            // {
            //     h.faction = targetData.targetType == TargetType.Player ? FactionType.Player :
            //         targetData.targetType == TargetType.Monster ? FactionType.Monster :
            //         FactionType.Neutral;
            // }

            Debug.Log($"target ID: {targetData.targetID}, 이름: {targetData.outterName}");
        }
    }

    // 상호 작용이 일어 났을 떄 반환할 정보
    public TargetData GetTargetData() // 이거를 나중에 써먹으면 됨
    {
        if  (targetData != null)
            Debug.Log("타겟 정보가 존재하지 않습니다.");
        
        return targetData != null ? targetData:  null;
    }
}