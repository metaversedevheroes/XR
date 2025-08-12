using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestGroupItem : MonoBehaviour
{
    [Header("Header")]
    public TMP_Text titleText;
    public Button toggleButton;

    [Header("Steps")]
    public GameObject stepsContainerRoot;  // StepsContainer (열고닫기 대상)
    public Transform stepsContainer;       // 스텝이 붙을 부모
    public QuestStepItem stepItemPrefab;

    private QuestGroupManager _gm;

    public void Bind(QuestGroupManager gm, QuestJournalUI journal)
    {
        _gm = gm;

        // 그룹 제목만
        titleText.text = gm.groupData.title;

        // 스텝들 생성
        for (int i = stepsContainer.childCount - 1; i >= 0; i--)
            Destroy(stepsContainer.GetChild(i).gameObject);

        foreach (var stepMgr in gm.stepManagers)
        {
            var row = Instantiate(stepItemPrefab, stepsContainer);
            row.Bind(stepMgr, gm);
        }

        // 열고닫기
        stepsContainerRoot.SetActive(true);
        toggleButton.onClick.RemoveAllListeners();
        toggleButton.onClick.AddListener(() =>
        {
            stepsContainerRoot.SetActive(!stepsContainerRoot.activeSelf);
        });
    }
    
    
}