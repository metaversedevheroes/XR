using UnityEngine;

[RequireComponent(typeof(Collider))]
public class MeleeHitbox : MonoBehaviour
{
    public int skillPower = 10;     // 스킬/무기 위력
    public LayerMask targetMask; 
    public Attacktype attacktype;

    public void Swing(GameObject attacker)
    {
        var center = transform.position + transform.forward * 1.0f;
        var hits = Physics.OverlapSphere(center, 1.5f, targetMask);

        foreach (var h in hits) {
            var defenderGO = h.gameObject;
            var ctx = new AttackContext(attacker, defenderGO, skillPower, h.ClosestPoint(center), attacktype);
            var result = DamageCalculator.Compute(ctx);

            var hp = defenderGO.GetComponent<Health>();
            if (hp != null) hp.ApplyDamage(result.finalDamage, ctx.hitPoint);
        }
    }
}