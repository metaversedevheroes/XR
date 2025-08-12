using UnityEngine;

public static class DamageCalculator
{
    public static DamageResult Compute(AttackContext ctx) {
        var atkProv = ctx.attacker.GetComponent<IAttackProvider>();
        var defProv = ctx.defender.GetComponent<IDefenseProvider>();

        int atk = (atkProv?.AttackPower ?? 0) + ctx.basePower; // 공격자 공격력 + 스킬 기본위력
        int def =  defProv?.Defense ?? 0;
        
        int raw = Mathf.Max(1, atk - def);

        // 지금 필요 없음
        // // 치명타
        // if (!crit && atkProv != null) {
        //     float p = atkProv.CritChance;
        //     if (UnityEngine.Random.value < p) crit = true;
        // }
        // float critMul = crit ? (atkProv?.CritMultiplier ?? 1.5f) : 1f;

        // 피해감소
        float reduce = defProv?.DamageReduction ?? 0f; // 0~1
        float afterCrit = raw ;
        int final = Mathf.Max(1, Mathf.RoundToInt(afterCrit * (1f - reduce)));

        return new DamageResult { finalDamage = final};
    }
}

