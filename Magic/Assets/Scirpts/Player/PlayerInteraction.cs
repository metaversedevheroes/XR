using UnityEngine;
using System.Collections.Generic;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 3f;

    private IInteractable _currentTarget;

    void Update()
    {
        DetectInteractable();

        if (Input.GetKeyDown(KeyCode.E) && _currentTarget != null)
        {
            List<InteractionOption> options = _currentTarget.GetAvailableInteractions();

            if (options == null || options.Count == 0)
            {
                _currentTarget.Interact(); // 기본 상호작용
            }
            else if (options.Count == 1)
            {
                // 어떤 대상에 할 지 띄우기
                options[0].Execute(gameObject, gameObject); // 유일한 선택 자동 실행 
            }
            else
            {
                // UI로 선택지 보여주기 추후 연결
                //InteractionUI.Instance.ShowOptions(options, _currentTarget);
            }
        }
    }

    void DetectInteractable()
    {
        Ray ray = new Ray(transform.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                _currentTarget = interactable;
                return;
            }
        }

        _currentTarget = null;
    }
}