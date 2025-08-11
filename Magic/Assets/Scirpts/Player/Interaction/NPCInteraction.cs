using UnityEngine;
using Yarn.Unity;

[RequireComponent(typeof(TargetIdentity))]
public class NPCInteraction : MonoBehaviour, IInteractable
{
    public TargetData targetData;
    private DialogueRouter router;

    void Awake()
    {
        router = DialogueRouter.Instance;
    }

    public string GetInteractText()
    {
        var name = GetComponent<TargetIdentity>().targetData.outterName;
        return $"talk to {name}";
    }

    public void Interact()
    {
        Debug.Log("NPC 와 상호작용 중");
        router.StartDialogueFor(targetData);
    }
}
