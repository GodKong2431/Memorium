using AYellowpaper.SerializedCollections;
using Mono.Cecil.Cil;
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

    public SerializedDictionary<PlayerStatType, float> BonusValues = new SerializedDictionary<PlayerStatType, float>
    {
        {PlayerStatType.HP, 0f},
        {PlayerStatType.MP, 0f},
        {PlayerStatType.HP_REGEN, 0f},
        {PlayerStatType.MP_REGEN, 0f},
        {PlayerStatType.ATK, 0f},
        {PlayerStatType.CRIT_MULT, 0f},
        {PlayerStatType.BOSS_DMG, 0f},
        {PlayerStatType.NORMAL_DMG, 0f},
    };


    public event Action OnLevelUp;
    public PlayerLevel(int level)
    {
        for (int i = 0; i < level; i++)
        {
            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey+i, out var value);

            BonusValues[PlayerStatType.ATK] += value.bonusAttack;
            BonusValues[PlayerStatType.MP] += value.bonusMP;
            BonusValues[PlayerStatType.MP_REGEN] += value.bonusMPRegen;
            BonusValues[PlayerStatType.HP] += value.bonusHP;
            BonusValues[PlayerStatType.HP_REGEN] += value.bonusHPRegen;
            BonusValues[PlayerStatType.CRIT_MULT] += value.bonusCriticalDamage;
            BonusValues[PlayerStatType.BOSS_DMG] += value.bonusBossDamage;
            BonusValues[PlayerStatType.NORMAL_DMG] += value.bonusNormalDamage;

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

            BonusValues[PlayerStatType.ATK] += value.bonusAttack;
            BonusValues[PlayerStatType.MP] += value.bonusMP;
            BonusValues[PlayerStatType.MP_REGEN] += value.bonusMPRegen;
            BonusValues[PlayerStatType.HP] += value.bonusHP;
            BonusValues[PlayerStatType.HP_REGEN] += value.bonusHPRegen;
            BonusValues[PlayerStatType.CRIT_MULT] += value.bonusCriticalDamage;
            BonusValues[PlayerStatType.BOSS_DMG] += value.bonusBossDamage;
            BonusValues[PlayerStatType.NORMAL_DMG] += value.bonusNormalDamage;
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
