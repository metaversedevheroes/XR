using UnityEngine;

public interface IDefenseProvider {
    int Defense { get; }         // 기초 방어력
    float DamageReduction { get; } // 피해감소(0~1)
}
