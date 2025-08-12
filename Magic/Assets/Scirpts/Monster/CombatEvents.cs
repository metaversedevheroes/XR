using System;
using UnityEngine;

public static class CombatEvents {
    public static event System.Action<AttackContext> OnAttackStarted; // 떄릴 때
    public static event System.Action<HitInfo>      OnHitLanded; // 맞을 떄
    
    

    // 공격할 떄 발생할 이벤트
    public static void RaiseAttackStarted(AttackContext ctxWithoutDefender)
        => OnAttackStarted?.Invoke(ctxWithoutDefender);

    // 실제로 맞았을 때 발생할 이벤트
    public static void RaiseHit(HitInfo hit)
        => OnHitLanded?.Invoke(hit);
}
