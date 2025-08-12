using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MeleeHitbox : MonoBehaviour
{
    [Header("무기 데미지")]
    public int skillPower = 10;
    [Header("공격 가능 반경")]
    public float radius = 1.5f; 
    [Header("공격 대상")]
    public LayerMask targetMask; 
    [Header("공격 타입")]
    public Attacktype attacktype; 

    private readonly HashSet<Health> _hitOnce = new();

    public void Swing(GameObject attacker)
    {
        // 공격 시작 이벤트
        var startCtx = new AttackContext(attacker, null, skillPower, transform.position, attacktype);
        CombatEvents.RaiseAttackStarted(startCtx);

        _hitOnce.Clear();

        var center = transform.position + transform.forward * 1.0f;
        var hits = Physics.OverlapSphere(center, radius, targetMask);

        foreach (var h in hits)
        {
            var hp = h.GetComponentInParent<Health>();
            if (hp == null || !hp.IsAlive) continue;
            if (_hitOnce.Contains(hp)) continue;

            // 공격자 속성 우선
            var atkProv = attacker.GetComponent<IAttackProvider>();
            var type = atkProv != null ? atkProv.Attacktype : attacktype;

            var hitPoint = h.ClosestPoint(center);
            var ctx = new AttackContext(attacker, hp.gameObject, skillPower, hitPoint, type);
            var result = DamageCalculator.Compute(ctx);

            hp.ApplyDamage(result.finalDamage, hitPoint);

            CombatEvents.RaiseHit(new HitInfo { context = ctx, result = result });

            _hitOnce.Add(hp);
        }
    }
}
