using UnityEngine;

public class MeleeAI : MonoBehaviour
{
    public Transform target;           // 플레이어 트랜스폼
    public MeleeHitbox hitbox;
    public float attackRange = 1.8f;
    public float cooldown = 1.0f;
    float last;

    void Update()
    {
        if (target == null) return;
        float d = Vector3.Distance(transform.position, target.position);
        if (d <= attackRange && Time.time - last >= cooldown) {
            last = Time.time;
            hitbox.Swing(gameObject);
        }
    }
}
