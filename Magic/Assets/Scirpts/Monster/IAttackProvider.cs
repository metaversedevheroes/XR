using UnityEngine;

public interface IAttackProvider {
    int AttackPower { get; }     // 기초 공격력
    float CritChance { get; }    // 치명타 확률
    float CritMultiplier { get; } // 치명 배수
    Attacktype Attacktype { get; } // 공격 타입
}

public enum Attacktype
{
    Fire,
    Ice, 
    Physical
}
