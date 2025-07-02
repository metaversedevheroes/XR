using UnityEngine;

public class DialogueTrigger : MonoBehaviour, IInteractable
{
    public DialogueGroupAsset dialogueGroup;

    public void Interact()
    {
        DialogueManager.Instance.StartDialogueGroup(dialogueGroup);
    }

    public string GetInteractText()
    {
        return "대화하기 (E)";
    }
}