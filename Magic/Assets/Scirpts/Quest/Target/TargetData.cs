using UnityEngine;

[CreateAssetMenu(fileName = "target_", menuName = "Game/Quest/Target")]
public class TargetData : ScriptableObject
{
    [Header("내부 식별자")]
    public string targetID;

    [Header("이름 정보")]
    public string outterName; // 사용자에게 보이는 이름
    public string innerName;  // 관리자용 식별 이름
}