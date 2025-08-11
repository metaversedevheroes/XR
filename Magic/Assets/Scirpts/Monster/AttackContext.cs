using UnityEngine;

public struct AttackContext {
    public GameObject attacker;       // 공격자
    public GameObject defender;       // 피격자
    public int basePower;             // 스킬/무기의 기본 위력
    public Vector3 hitPoint;          // 충돌 지점(이펙트용)
    public Attacktype attacktype;     // 공격 타입(이펙트 연결)

    public AttackContext(
        GameObject atk, 
        GameObject def, 
        int power, 
        Vector3 point, 
        Attacktype atty
        ) 
    {
        attacker = atk; 
        defender = def; 
        basePower = power; 
        hitPoint = point; 
        attacktype = atty;
    }
}