using UnityEngine;

// 실제 player 쪽에 있으면 됨 // interacttrigger 을 찾을 수 있는 위치에 존재해야함
public class PlayerInitializer : MonoBehaviour
{
    void Start()
    {
        var trigger = GetComponent<InteractTrigger>();
        trigger.OnEnter += interactable =>
        {
            InteractionUIManager.Instance.ShowPrompt(interactable.GetInteractText());
        };
        trigger.OnExit += () =>
        {
            InteractionUIManager.Instance.HidePrompt();
        };
    }
}