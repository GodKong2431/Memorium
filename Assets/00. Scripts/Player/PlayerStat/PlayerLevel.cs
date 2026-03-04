using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

[System.Serializable]

public class PlayerLevel
{
    private CurrencyType currencyType = CurrencyType.Exp;

    private int levelKey = 1020001;
    private int playerExpKey = 1510000;

    public int CurrentLevel;

    public BigDouble RequiredExp;

    public int BonusCristal;

    public SerializedDictionary<StatType, float> BonusValues = new SerializedDictionary<StatType, float>
    {
        {StatType.HP, 0f},
        {StatType.MP, 0f},
        {StatType.HP_REGEN, 0f},
        {StatType.MP_REGEN, 0f},
        {StatType.ATK, 0f},
        {StatType.CRIT_MULT, 0f},
        {StatType.BOSS_DMG, 0f},
        {StatType.NORMAL_DMG, 0f},
    };


    public event Action OnLevelUp;
    public PlayerLevel(int level)
    {
        for (int i = 0; i < level; i++)
        {
            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey+i, out var value);

            BonusValues[StatType.ATK] += value.bonusAttack;
            BonusValues[StatType.MP] += value.bonusMP;
            BonusValues[StatType.MP_REGEN] += value.bonusMPRegen;
            BonusValues[StatType.HP] += value.bonusHP;
            BonusValues[StatType.HP_REGEN] += value.bonusHPRegen;
            BonusValues[StatType.CRIT_MULT] += value.bonusCriticalDamage;
            BonusValues[StatType.BOSS_DMG] += value.bonusBossDamage;
            BonusValues[StatType.NORMAL_DMG] += value.bonusNormalDamage;

            BonusCristal += value.bonusCristal;
        }

        CurrentLevel = level;

        playerExpKey += level;

        levelKey = (level-1) + levelKey;

        DataManager.Instance.PlayerLevelDict.TryGetValue(playerExpKey, out var playerExpTable);

        RequiredExp = playerExpTable.reqExp;
    }

    public void ExpCheck(CurrencyType currencyType, BigDouble currentExp)
    {

        //var currentExp = currency.GetAmount(CurrencyType.Exp);

        if (this.currencyType != currencyType)
        {
            return;
        }

        var currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currencyModule == null)
            return;

        while (currencyModule.TrySpendSilent(CurrencyType.Exp, RequiredExp))
        {
            levelKey++;

            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey, out var value);

            if (value == null)
            {
                break;
            }

            BonusValues[StatType.ATK] += value.bonusAttack;
            BonusValues[StatType.MP] += value.bonusMP;
            BonusValues[StatType.MP_REGEN] += value.bonusMPRegen;
            BonusValues[StatType.HP] += value.bonusHP;
            BonusValues[StatType.HP_REGEN] += value.bonusHPRegen;
            BonusValues[StatType.CRIT_MULT] += value.bonusCriticalDamage;
            BonusValues[StatType.BOSS_DMG] += value.bonusBossDamage;
            BonusValues[StatType.NORMAL_DMG] += value.bonusNormalDamage;
            BonusCristal += value.bonusCristal;

            playerExpKey++;

            DataManager.Instance.PlayerLevelDict.TryGetValue(playerExpKey, out var playerExpTable);

            RequiredExp = playerExpTable.reqExp;

            CurrentLevel++;

            if (CurrentLevel > 30)
            {
                currencyModule.AddCurrency(CurrencyType.TraitPoint, 1);
            }

            OnLevelUp?.Invoke();
        }
    }
}
