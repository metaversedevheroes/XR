using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// 추후 보완 필요
public class QuestSystem : MonoBehaviour
{
    public static QuestSystem Instance { get; private set; }

    // 순차 진행할 퀘스트 그룹들을 큐로 보관
    [Header("초기 퀘스트 그룹들")]
    public List<QuestGroupData> initialQuestsList;
    private Queue<QuestGroupData> initialQuestsQueue;

    // 현재 활성화된 퀘스트 매니저들
    private Dictionary<string, QuestGroupManager> activeQuests = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        // DontDestroyOnLoad(gameObject);

        // List → Queue
        initialQuestsQueue = new Queue<QuestGroupData>(initialQuestsList);
        StartNextQuest();
    }

    /// <summary>
    /// 큐에서 다음 퀘스트를 꺼내 등록하고,
    /// 완료되면 자동으로 다음 퀘스트를 시작하도록 연결합니다. // 추후 이거 보완 필요
    /// </summary>
    public void StartNextQuest()
    {
        if (initialQuestsQueue.Count == 0) return;

        var nextQuestData = initialQuestsQueue.Dequeue();
        StartQuest(nextQuestData);
    }

    public void StartQuest(QuestGroupData questData)
    {
        if (activeQuests.ContainsKey(questData.questID)) return;

        var gm = new QuestGroupManager(questData);
        activeQuests.Add(questData.questID, gm);
        Debug.Log($"[퀘스트 등록] {questData.title}");

        // 퀘스트가 완료되면 자동으로 다음 퀘스트 시작
        gm.OnQuestCompleted += StartNextQuest;
    }
    
    /// <summary>
    /// 모든 활성화된 QuestGroupManager를 반환
    /// </summary>
    public IEnumerable<QuestGroupManager> GetAllQuestManagers()
        => activeQuests.Values;

    /// <summary>
    /// 이 NPC(targetID)와 연관된 활성 퀘스트 매니저를 하나만 반환
    /// (예: 메인 퀘스트 우선순위 등 추가 로직 가능)
    /// </summary>
    public QuestGroupManager GetQuestForNPC(string targetID)
    {
        Debug.Log($"{targetID}로 비교중!!!");
        return activeQuests.Values
            .FirstOrDefault(qm =>
                qm.groupData.linkedNpc?.targetID == targetID ||
                qm.groupData.steps.Any(s => s.target.targetID == targetID)
            );
    }

    public QuestGroupManager GetQuestManager(string questID)
    {
        activeQuests.TryGetValue(questID, out var gm);
        return gm;
    }
    
    // 외부에서 연락 받는 곳
    public void ReportProgress(string targetID, QuestStepType stepType)
    {
        foreach (var qm in activeQuests.Values)
        {
            if (qm.TryTriggerBy(targetID, stepType))
            {
                Debug.Log($"[퀘스트] {targetID}/{stepType} 트리거 처리됨 → {qm.groupData.title}");
                break; // 1개만 처리(필요시 정책에 따라 여러 개 처리도 가능)
            }
        }
    }

    
}
