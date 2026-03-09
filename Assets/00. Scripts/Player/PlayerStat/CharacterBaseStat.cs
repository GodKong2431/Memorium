using AYellowpaper.SerializedCollections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class CharacterBaseStat
{
    [Header("Class")]
    [Tooltip("플레이어의 클래스")]
    [SerializeField] private ClassType classType;

    // 기본 스탯

    // 이벤트
    //public event Action<PlayerStatType,float> StatChanged;

    public ClassType CurrentClass { get { return classType;  } }
    
    private CharacterBaseStatInfoTable statTable;

    [SerializedDictionary("Base Stat Type", "Value")]
    public SerializedDictionary<StatType, float> baseStatValues = new SerializedDictionary<StatType, float>();

    public CharacterBaseStat(int key)
    {
        if (!DataManager.Instance.CharacterBaseStatInfoDict.TryGetValue(key, out statTable))
        {

            return;
        }
        SetBaseStat();
    }

    // 스탯 설정
    public void SetBaseStat()
    {
        classType = statTable.classType;

        baseStatValues.Clear();
        baseStatValues.Add(StatType.HP, statTable.baseHP);
        baseStatValues.Add(StatType.HP_REGEN, statTable.baseHPRegen);
        baseStatValues.Add(StatType.MP, statTable.baseMP);
        baseStatValues.Add(StatType.MP_REGEN, statTable.baseMPRegen);
        baseStatValues.Add(StatType.ATK, statTable.baseAttack);
        baseStatValues.Add(StatType.ATK_SPEED, statTable.baseAttackSpeed);
        baseStatValues.Add(StatType.PHYS_DEF, statTable.basePhysicalResist);
        baseStatValues.Add(StatType.MAGIC_DEF, statTable.baseMagicResist);
        baseStatValues.Add(StatType.CRIT_CHANCE, statTable.baseCritical);
        baseStatValues.Add(StatType.CRIT_MULT, statTable.baseCriticalMultiPlier);
        baseStatValues.Add(StatType.MOVE_SPEED, statTable.baseMoveSpeed);
        baseStatValues.Add(StatType.COOLDOWN_REDUCE, statTable.baseCooltimeRegen);
        baseStatValues.Add(StatType.GOLD_GAIN, statTable.baseMoneyGain);
        baseStatValues.Add(StatType.EXP_GAIN, statTable.baseExpGain);
        baseStatValues.Add(StatType.BOSS_DMG, statTable.baseBossDamage);
        baseStatValues.Add(StatType.NORMAL_DMG, statTable.baseNormalDamage);
        baseStatValues.Add(StatType.DMG_MULT, statTable.baseFinalMultiPlier);
    }
}
