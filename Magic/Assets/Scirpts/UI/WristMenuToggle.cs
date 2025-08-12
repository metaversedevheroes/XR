using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class WristMenuToggle : MonoBehaviour
{
    [SerializeField] private GameObject wristMenu;
    [SerializeField] private InputActionReference toggleAction;

    private InputAction _action;
    private bool _createdByScript;

    void Awake()
    {
        if (!wristMenu) wristMenu = gameObject;

        if (toggleAction && toggleAction.action != null)
        {
            _action = toggleAction.action;
        }
        else
        {
            _action = new InputAction(type: InputActionType.Button,
                binding: "<XRController>{LeftHand}/primaryButton"); // X 버튼
            _createdByScript = true;
        }
    }

    void OnEnable()
    {
        _action.Enable();
        _action.performed += OnToggle;
    }

    void OnDisable()
    {
        // 콜백 중에 비활성화되면 상태 파괴가 겹쳐 오류가 나므로 Dispose는 하지 않음
        _action.performed -= OnToggle;
        _action.Disable();
    }

    void OnDestroy()
    {
        if (_createdByScript && _action != null)
            _action.Dispose(); // 여기서만 파괴
    }

    private void OnToggle(InputAction.CallbackContext _)
    {
        // 콜백 프레임이 끝난 뒤에 비활성화하도록 지연
        StartCoroutine(ToggleNextFrame());
    }

    private IEnumerator ToggleNextFrame()
    {
        yield return null; // 다음 프레임
        if (wristMenu) wristMenu.SetActive(!wristMenu.activeSelf);
    }
}