using UnityEngine;

public class PlayerStats : MonoBehaviour, IAttackProvider, IDefenseProvider
{
    [Header("Attack")]
    public int attackPower = 15;
    public float critChance = 0.1f;
    public float critMultiplier = 1.5f;
    public Attacktype attacktype = Attacktype.Ice;

    [Header("Defense")]
    public int defense = 5;
    public float damageReduction = 0.1f;

    public int AttackPower => attackPower;
    public float CritChance => critChance;
    public float CritMultiplier => critMultiplier;
    public Attacktype Attacktype => attacktype;

    public int Defense => defense;
    public float DamageReduction => damageReduction;
}