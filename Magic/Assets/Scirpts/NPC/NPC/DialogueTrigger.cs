using UnityEngine;
using System.Collections.Generic;
using System.Transactions;
public class DialogueTrigger : MonoBehaviour, IInteractable
{
    public DialogueGroupAsset dialogueGroup;

    public string GetInteractText() => "대화하기 (E)";

    public void Interact()
    {
        DialogueManager.Instance.StartDialogueGroup(dialogueGroup);
    }

    public List<InteractionOption> GetAvailableInteractions()
    {
        return new List<InteractionOption>
        {
            // new InteractionOption
            // {
            //     actionID = "talk",
            //     displayText = "대화하기",
            //     key = KeyCode.E
            // }
        };
    }
}
