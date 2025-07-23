using UnityEngine;

public class TargetIdentity : MonoBehaviour
{
    [Header("이 target의 데이터")]
    public TargetData targetData;

    // 사실 처음에 이렇게 반환할 필요는 없음
    void Start()
    {
        if (targetData != null)
        {
            Debug.Log($"target ID: {targetData.targetID}, 이름: {targetData.outterName}");
        }
    }

    public string GetTargetData() // 이거를 나중에 써먹으면 됨
    {
        return targetData != null ? targetData.outterName : "유진님, 죄송하지만 targetData의 정보를 알 수 없습니다.";
    }
    
}