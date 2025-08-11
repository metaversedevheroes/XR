using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IInteractable
{
    public DialogueGroupAsset dialogueGroup;

    public void Interact() // 플레이어가 e 키 누르면 실행될 함수
    {
        DialogueManager.Instance.StartDialogueGroup(dialogueGroup);
    }

    public string GetInteractText() // 화면에 뜰 UI 문구
    {
        return "대화하기 (E)";
    }
}