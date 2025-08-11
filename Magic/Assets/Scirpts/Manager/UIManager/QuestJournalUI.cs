using System.Linq;
using UnityEngine;

public class QuestJournalUI : MonoBehaviour
{
    public QuestSystem questSystem;           // 씬의 QuestSystem (비워두면 Instance로 잡음)
    public Transform groupsContainer;         // ScrollView의 Content 같은 Transform
    public QuestGroupItem groupItemPrefab;    // 프리팹

    void OnEnable() => Refresh();

    public void Refresh()
    {
        Debug.Log("Refresh");
        if (questSystem == null) questSystem = QuestSystem.Instance;
        
        for (int i = groupsContainer.childCount - 1; i >= 0; i--)
            Destroy(groupsContainer.GetChild(i).gameObject);

        // 진행 중(미완료)인 퀘스트만
        var managers = questSystem.GetAllQuestManagers().Where(gm => !gm.IsQuestCompleted);
        foreach (var gm in managers)
        {
            Debug.Log($"{gm}");
            var groupItem = Instantiate(groupItemPrefab, groupsContainer);
            groupItem.Bind(gm, this);
        }
    }
}
