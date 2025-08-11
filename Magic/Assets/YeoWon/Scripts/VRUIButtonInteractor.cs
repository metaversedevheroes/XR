using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class VRUIButtonInteractor : MonoBehaviour
{
    public Transform rayOrigin;                      // 컨트롤러 Transform
    public InputActionProperty triggerAction;        // 트리거 액션 (예: XRI RightHand Interaction/Select)
    public float maxDistance = 5f;
    public LayerMask uiLayerMask;

    private Button currentButton;

    void Update()
    {
        DetectButton();

        if (triggerAction.action.WasPressedThisFrame() && currentButton != null)
        {
            currentButton.onClick.Invoke(); // ✅ 실제로 버튼 클릭한 것처럼 동작
        }
    }

    void DetectButton()
    {
        currentButton = null;

        Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, uiLayerMask))
        {
            currentButton = hit.collider.GetComponent<Button>();
        }
    }
}