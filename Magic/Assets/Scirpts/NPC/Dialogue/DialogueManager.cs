using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    [Header("UI 요소")]
    public GameObject dialogueUI;
    public TMP_Text dialogueText;
    public Button nextButton;

    private Queue<DialogueData> dialogueQueue;

    void Awake()
    {
        // 싱글톤으로 설정
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        dialogueQueue = new Queue<DialogueData>();
    }

    /// <summary>
    /// DialogueGroup을 받아서 대사 시작
    /// </summary>
    public void StartDialogueGroup(DialogueGroupAsset group)
    {
        dialogueQueue.Clear();

        foreach (var dialogue in group.dialogues)
        {
            dialogueQueue.Enqueue(dialogue);
        }

        dialogueUI.SetActive(true);
        ShowNextDialogue();
    }

    public void ShowNextDialogue()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueData current = dialogueQueue.Dequeue();
        dialogueText.text = current.text;

        // 다음 대사 버튼 활성화
        nextButton.onClick.RemoveAllListeners();
        nextButton.onClick.AddListener(ShowNextDialogue);
    }

    public void EndDialogue()
    {
        dialogueUI.SetActive(false);
        dialogueText.text = "";
    }
}