using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public enum StatType
{
    HP,HPRegen,
    ATK, ATKSpeed,
    DEF, MagicDEF,
    MP, MPRegen,
    CritChance, CritMult,
    CoolDown,
    MoveSpeed,
    ExpGain, GoldGain
}

public class PlayerStat : MonoBehaviour
{
    // 기본 스탯
    [Header("Health")]
    [Tooltip("플레이어의 체력")]
    [SerializeField] private float baseHealthPoint;
    [Tooltip("플레이어의 체력 재생")]
    [SerializeField] private float baseHpRegeneration;

    [Header("Attack")]
    [Tooltip("플레이어의 공격력")]
    [SerializeField] private float baseAttack;
    [Tooltip("플레이어의 공격속도")]
    [SerializeField] private float baseAttackSpeed;

    [Header("Resistance")]
    [Tooltip("플레이어의 물리저항력")]
    [SerializeField] private float baseDefense;
    [Tooltip("플레이어의 마법저항력")]
    [SerializeField] private float baseMagicResistance;

    [Header("Mana")]
    [Tooltip("플레이어의 마나")]
    [SerializeField] private float baseMana;
    [Tooltip("플레이어의 마나재생")]
    [SerializeField] private float baseManaRegeneration;

    [Header("Critical")]
    [Tooltip("플레이어의 치명타 확률")]
    [SerializeField] private float baseCriticalChance;
    [Tooltip("플레이어의 치명타 배율")]
    [SerializeField] private float baseCriticalMultiplier;

    [Header("Skill")]
    [Tooltip("플레이어의 쿨타임")]
    [SerializeField] private float baseCoolDown;

    [Header("Speed")]
    [Tooltip("플레이어의 이동속도")]
    [SerializeField] private float baseMoveSpeed;

    [Header("Gain")]
    [Tooltip("플레이어의 골드 획득량")]
    [SerializeField] private float baseGoldGain;
    [Tooltip("플레이어의 경험치 획득량")]
    [SerializeField] private float baseExpGain;

    // 이벤트
    public event Action<StatType,float> StatChanged;

    public float HealthPoint { get { return baseHealthPoint; } }
    public float HpRegeneration { get { return baseHpRegeneration; } }
    public float Attack {  get { return baseAttack; } }
    public float AttackSpeed { get { return baseAttackSpeed; } }
    public float Defense { get { return baseDefense; } }
    public float MagicResistance { get { return baseMagicResistance; } }
    public float Mana { get { return baseMana; } }
    public float ManaRegeneration { get { return baseManaRegeneration;} }
    public float CriticalChance { get { return baseCriticalChance; } }
    public float CriticalMultiplier { get { return baseCriticalMultiplier; } }
    public float CoolDown { get { return baseCoolDown; } }
    public float MoveSpeed { get { return baseMoveSpeed; } }
    public float ExpGain { get { return baseExpGain; } }
    public float GoldGain { get { return baseGoldGain; } }

    // 스탯 가져오기
    public float GetStat(StatType type)
    {
        return type switch
        {
            StatType.HP => baseHealthPoint,
            StatType.HPRegen => baseHpRegeneration,
            StatType.ATK => baseAttack,
            StatType.ATKSpeed => baseAttackSpeed,
            StatType.DEF => baseDefense,
            StatType.MagicDEF => baseMagicResistance,
            StatType.MP => baseMana,
            StatType.MPRegen => baseManaRegeneration,
            StatType.CritChance => baseCriticalChance,
            StatType.CritMult => baseCriticalMultiplier,
            StatType.CoolDown => baseCoolDown,
            StatType.MoveSpeed => baseMoveSpeed,
            StatType.ExpGain => baseExpGain,
            StatType.GoldGain => baseGoldGain,
            _ => 0f
        };
    }

    // 스탯 설정
    public void SetStat(StatType statType, float value)
    {
        switch (statType)
        {
            case StatType.HP:
                Set(ref baseHealthPoint, value, statType);
                break;
            case StatType.HPRegen:
                Set(ref baseHpRegeneration, value, statType);
                break;
            case StatType.ATK:
                Set(ref baseAttack, value, statType);
                break;
            case StatType.ATKSpeed:
                Set(ref baseAttackSpeed, value, statType);
                break;
            case StatType.DEF:
                Set(ref baseDefense, value, statType);
                break;
            case StatType.MagicDEF:
                Set(ref baseMagicResistance, value, statType);
                break;
            case StatType.MP:
                Set(ref baseMana, value, statType);
                break;
            case StatType.MPRegen:
                Set(ref baseManaRegeneration, value, statType);
                break;
            case StatType.CritChance:
                Set(ref baseCriticalChance, value, statType);
                break;
            case StatType.CritMult:
                Set(ref baseCriticalMultiplier, value, statType);
                break;
            case StatType.CoolDown:
                Set(ref baseCoolDown, value, statType);
                break;
            case StatType.MoveSpeed:
                Set(ref baseMoveSpeed, value, statType);
                break;
            case StatType.ExpGain:
                Set(ref baseExpGain, value, statType);
                break;
            case StatType.GoldGain:
                Set(ref baseGoldGain, value, statType);
                break;
            default:
                Debug.Log($"{statType.ToString()}에 해당하는 스탯이 없습니다");
                break;
        }
    }

    // 값 변동
    private void Set(ref float target, float value, StatType type)
    {
        target = value;
        StatChanged?.Invoke(type, target);
    }
    
    // 더하기
    public void AddStat(float value, StatType type)
    {
        SetStat(type, GetStat(type) + value);
    }

    // 곱하기
    public void MulStat(float value, StatType type)
    {
        SetStat(type, GetStat(type) * value);
    }
}
