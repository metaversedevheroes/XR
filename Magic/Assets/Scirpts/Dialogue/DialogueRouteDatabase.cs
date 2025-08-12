using UnityEngine;

[CreateAssetMenu(menuName = "Game/Dialogue/Route Database", fileName = "DialogueRouteDatabase_")]
public class DialogueRouteDatabase : ScriptableObject
{
    [Tooltip("모든 DialogueRoute 에셋을 관리")]
    public DialogueRoute[] routes;
}