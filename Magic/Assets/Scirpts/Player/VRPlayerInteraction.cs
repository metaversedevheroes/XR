using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRPlayerInteraction : MonoBehaviour
{
    [Header("Ray sources (선택)")]
    public XRRayInteractor rightRay;   // XRI Ray Interactor (오른손)
    public XRRayInteractor leftRay;    // XRI Ray Interactor (왼손)

    [Header("Fallback ray origins (선택)")]
    public Transform rightController;  // 없으면 rightRay 사용
    public Transform leftController;   // 없으면 leftRay 사용
    public float maxDistance = 100f;

    [Header("Input (XRI InputActionProperty)")]
    // 인스펙터에서 각각 "XRI Right Interaction/Select", "XRI Left Interaction/Select" 로 바인드
    public InputActionProperty rightSelect; 
    public InputActionProperty leftSelect;

    private void OnEnable()
    {
        if (rightSelect.action != null) rightSelect.action.performed += OnRightSelect;
        if (leftSelect.action != null)  leftSelect.action.performed  += OnLeftSelect;

        if (rightSelect.action != null) rightSelect.action.Enable();
        if (leftSelect.action != null)  leftSelect.action.Enable();
    }

    private void OnDisable()
    {
        if (rightSelect.action != null) rightSelect.action.performed -= OnRightSelect;
        if (leftSelect.action != null)  leftSelect.action.performed  -= OnLeftSelect;

        if (rightSelect.action != null) rightSelect.action.Disable();
        if (leftSelect.action != null)  leftSelect.action.Disable();
    }

    private void OnRightSelect(InputAction.CallbackContext _)
    {
        TryInteract(fromRightHand: true);
    }

    private void OnLeftSelect(InputAction.CallbackContext _)
    {
        TryInteract(fromRightHand: false);
    }

    private void TryInteract(bool fromRightHand)
    {
        // 1) XRRayInteractor가 있으면 그것부터 사용 (UI/3D 레이 모두 대응)
        XRRayInteractor ray = fromRightHand ? rightRay : leftRay;
        if (ray != null && ray.enabled)
        {
            if (ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
            {
                TryHandle(hit.collider?.gameObject);
                return;
            }
            // 2D/UIDOM과 상호작용 중이면 3D 히트가 없을 수 있음 → 아래 폴백 계속 진행
        }

        // 2) 폴백: 컨트롤러 트랜스폼 전방으로 물리 레이캐스트
        Transform origin = fromRightHand ? rightController : leftController;
        if (origin != null)
        {
            if (Physics.Raycast(origin.position, origin.forward, out RaycastHit hit, maxDistance))
            {
                TryHandle(hit.collider?.gameObject);
            }
        }
    }

    // private void TryHandle(GameObject go) {
    //     if (!go) return;
    //     var interactable = go.GetComponentInParent<IInteractable>(); // ← InParent 추천
    //     if (interactable != null) interactable.Interact();
    // }

    private void TryHandle(GameObject go)
    {
        if (go == null) return;
    
        var interactable = go.GetComponent<IInteractable>();
        if (interactable != null)
        {
            Debug.Log($"상호작용 대상: {go.name}");
            interactable.Interact();
        }
        else
        {
            Debug.Log($"IInteractable 없음: {go.name}");
        }
    }
}
