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
    public SerializedDictionary<PlayerStatType, float> baseStatValues = new SerializedDictionary<PlayerStatType, float>();

    public CharacterBaseStat(int key)
    {
        if (!DataManager.Instance.CharacterBaseStatInfoDict.TryGetValue(key, out statTable))
        {
            Debug.Log($"[CharacterBaseStat] [{key}] ID 값에 해당하는 데이터가 없습니다 ");
            return;
        }
        SetBaseStat();
    }

    // 스탯 설정
    public void SetBaseStat()
    {
        classType = statTable.classType;

        baseStatValues.Clear();
        baseStatValues.Add(PlayerStatType.HP, statTable.baseHP);
        baseStatValues.Add(PlayerStatType.HP_REGEN, statTable.baseHPRegen);
        baseStatValues.Add(PlayerStatType.MP, statTable.baseMP);
        baseStatValues.Add(PlayerStatType.MP_REGEN, statTable.baseMPRegen);
        baseStatValues.Add(PlayerStatType.ATK, statTable.baseAttack);
        baseStatValues.Add(PlayerStatType.ATK_SPEED, statTable.baseAttackSpeed);
        baseStatValues.Add(PlayerStatType.PHYS_DEF, statTable.basePhysicalResist);
        baseStatValues.Add(PlayerStatType.MAGIC_DEF, statTable.baseMagicResist);
        baseStatValues.Add(PlayerStatType.CRIT_CHANCE, statTable.baseCritical);
        baseStatValues.Add(PlayerStatType.CRIT_MULT, statTable.baseCriticalMultiPlier);
        baseStatValues.Add(PlayerStatType.MOVE_SPEED, statTable.baseMoveSpeed);
        baseStatValues.Add(PlayerStatType.COOLDOWN_REDUCE, statTable.baseCooltimeRegen);
        baseStatValues.Add(PlayerStatType.GOLD_GAIN, statTable.baseMoneyGain);
        baseStatValues.Add(PlayerStatType.EXP_GAIN, statTable.baseExpGain);
        baseStatValues.Add(PlayerStatType.BOSS_DMG, statTable.baseBossDamage);
        baseStatValues.Add(PlayerStatType.NORMAL_DMG, statTable.baseNormalDamage);
        baseStatValues.Add(PlayerStatType.DMG_MULT, statTable.baseFinalMultiPlier);
    }
}

