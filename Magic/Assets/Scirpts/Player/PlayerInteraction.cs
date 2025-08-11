using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public Camera mainCamera;
    public float maxDistance = 100f;

    // private PlayerInputActions inputActions; // 지금 이게 어딨는 지 모름 
    // 클릭에 vr 관련 행동 추가하고 
    // 움직임이 있는 스크립트나 따로 빼면 됨

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main; // 이 부분 나중에 멀티로 바뀌면 오류가 좀 날 수도?? 

        //inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        //inputActions.Player.Enable();
        //inputActions.Player.Click.performed += OnClickPerformed;
    }

    private void OnDisable()
    {
        //inputActions.Player.Click.performed -= OnClickPerformed;
        //inputActions.Player.Disable();
    }

    private void OnClickPerformed(InputAction.CallbackContext context)
    {
        
        Debug.Log("Click 했음");
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance))
        {
            Debug.Log($"{hit.collider.gameObject.name}입니다."); // 이 부분을 이름이 아니라 상호작용 가능한 객체인지 확인하고 해당 사항 반환하게 하기
            
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                HandleInteraction(interactable, hit.collider.gameObject);
            }
        }
    }

    private void HandleInteraction(IInteractable interactable, GameObject target)
    {   
        Debug.Log("상호작용 한다잉");
        // 추후 뭔가가 추가 되면 여기서 어떤 행동을 하지 등 분기 나뉘면 될 듯
        interactable.Interact();
    }
}