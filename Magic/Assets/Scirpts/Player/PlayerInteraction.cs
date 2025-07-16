using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 3f;
    public string interactText;

    private IInteractable _currentTarget;

    void Update()
    {
        DetectInteractable();

        if (Input.GetKeyDown(KeyCode.E) && _currentTarget != null)
        {
            _currentTarget.Interact();
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
                //interactText.text = interactable.GetInteractText();
                //interactText.gameObject.SetActive(true);
                return;
            }
        }

        _currentTarget = null;
        //interactText.gameObject.SetActive(false);
    }
}