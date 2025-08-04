using UnityEngine;

[CreateAssetMenu(menuName = "Interaction/TalkOption")]
public class TalkOption : InteractionOption
{
    public DialogueGroupAsset dialogueGroup;

    public override void Execute(GameObject interactor, GameObject target)
    {
        // 대화 대상이 맞는지 확인
        if (dialogueGroup != null)
        {
            //DialogueManager.Instance.StartDialogueGroup(dialogueGroup, interactor, target);
        }
        else
        {
            Debug.LogWarning($"대화 그룹이 지정되지 않았습니다: {target.name}");
        }
    }
}
