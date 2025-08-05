using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public Camera mainCamera;
    public float maxDistance = 5f;

    private PlayerInputActions inputActions;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Click.performed += OnClickPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Click.performed -= OnClickPerformed;
        inputActions.Player.Disable();
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                HandleInteraction(interactable, hit.collider.gameObject);
            }
        }
    }

    private void HandleInteraction(IInteractable interactable, GameObject target)
    {
        List<InteractionOption> options = interactable.GetAvailableInteractions();

        if (options == null || options.Count == 0)
        {
            Debug.Log("기본 상호작용 실행");
            interactable.Interact();
        }
        else if (options.Count == 1)
        {
            Debug.Log("단일 옵션 자동 실행");
            options[0].Execute(gameObject, target);
        }
        else
        {
            Debug.Log("여러 옵션 있음 - UI로 선택 필요");
            // InteractionUI.Instance.ShowOptions(options, interactable);
        }
    }
}