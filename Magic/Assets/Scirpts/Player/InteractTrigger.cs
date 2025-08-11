using UnityEngine;

// 충돌을 감지하는 콜라이더 쪽에 붙이면 됨 // 상호작용 할 수 있는 범위에 들어오면 화면에 말 띄움
public class InteractTrigger : MonoBehaviour
{
    public event System.Action<IInteractable> OnEnter;
    public event System.Action OnExit;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("PYJ_OnTriggerEnter");
        var interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
            OnEnter?.Invoke(interactable);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("PYJ_OnTriggerExit");
        if (other.GetComponent<IInteractable>() != null)
            OnExit?.Invoke();
    }
}