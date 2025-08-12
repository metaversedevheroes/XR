using UnityEngine;
using System;

[RequireComponent(typeof(Collider))]
public class Health : MonoBehaviour
{
    public int maxHP = 100; 
    public int CurrentHP { get; private set; }
    public bool IsAlive => CurrentHP > 0;

    public event Action<int, Vector3> OnDamaged; // (피해량, 히트포인트, 크리티컬)
    public event Action OnDied;

    void Awake() => CurrentHP = maxHP;

    // "최종 피해"를 받는다. (계산은 이미 외부) 여긴 적용만
    public void ApplyDamage(int finalDamage, Vector3 hitPoint) {
        if (!IsAlive) return;

        CurrentHP = Mathf.Max(0, CurrentHP - finalDamage);
        OnDamaged?.Invoke(finalDamage, hitPoint);

        if (CurrentHP == 0) {
            OnDied?.Invoke();
        }
    }
}