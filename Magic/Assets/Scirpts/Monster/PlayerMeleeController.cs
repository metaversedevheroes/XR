using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMeleeController : MonoBehaviour
{
    public MeleeHitbox hitbox; 
    public InputActionProperty attack; // Player/Attack 액션 바인딩

    void OnEnable() { attack.action.performed += OnAttack; attack.action.Enable(); }
    void OnDisable(){ attack.action.performed -= OnAttack; attack.action.Disable(); }

    void OnAttack(InputAction.CallbackContext _)
    {
        if (hitbox != null) hitbox.Swing(gameObject);
    }
}