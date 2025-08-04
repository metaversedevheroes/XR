using System.Collections.Generic;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }

    private Dictionary<string, QuestGroupManager> activeQuests = new();

    [Header("초기 퀘스트 그룹들")]
    //이걸 이렇게 담는 건 ㅇㅈ , 이걸 큐로 바꿔야 하나??
    public List<QuestGroupData> initialQuests;
    // 아님 이거랑 별개로 활성화된 퀘스트만을 관리하는 걸 따로 가지고 있어야 하나??

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeQuests();
    }

    private void InitializeQuests()
    {
        foreach (var questData in initialQuests)
        {
            StartQuest(questData);
        }
    }

    public void StartQuest(QuestGroupData questData)
    {
        if (!activeQuests.ContainsKey(questData.questID))
        {
            var groupManager = new QuestGroupManager(questData);
            activeQuests.Add(questData.questID, groupManager);
            Debug.Log($"[퀘스트 등록] {questData.title}");
        }
    }

    public void InProgress()
    {
        // 등록된 퀘스트 중 현재 진행 중인 퀘스트를 따로 관리/ 큐에 담거나 순차적으로 딱딱 나올 수 있게 하기??
        Debug.Log($"[퀘스트 진행] 뭐하는지 나중에 변수로 입력");
    }

    public void RegisterInteraction(string questID, string stepID)
    {
        if (activeQuests.TryGetValue(questID, out var manager))
        {
            manager.RegisterInteraction(stepID);

            if (manager.IsQuestCompleted)
            {
                Debug.Log($"[퀘스트 완료] {manager.groupData.title}");
                // 보상 지급 처리 등 여기에 추가
            }
        }
    }

    public bool IsQuestCompleted(string questID)
    {
        return activeQuests.ContainsKey(questID) && activeQuests[questID].IsQuestCompleted;
    }

    public QuestGroupManager GetQuestManager(string questID)
    {
        activeQuests.TryGetValue(questID, out var manager);
        return manager;
    }
}