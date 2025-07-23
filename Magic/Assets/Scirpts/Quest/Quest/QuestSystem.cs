using System.Collections.Generic;
using UnityEngine;

public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }

    private Dictionary<string, QuestGroupManager> activeQuests = new();

    [Header("초기 퀘스트 그룹들")]
    public List<QuestGroupData> initialQuests;

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
            Debug.Log($"[퀘스트 시작] {questData.title}");
        }
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