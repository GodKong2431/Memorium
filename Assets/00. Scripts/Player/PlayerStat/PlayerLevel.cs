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

    public int BonusAttack;
    public int BonusMP;
    public int BonusMPRegen;
    public int BonusHP;
    public int BonusHPRegen;
    public float BonusCriticalDamage;
    public float BonusBossDamage;
    public float BonusNormalDamage;
    public int BonusCristal;

    public event Action OnLevelUp;
    public PlayerLevel(int level)
    {
        for (int i = 0; i < level; i++)
        {
            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey+i, out var value);

            BonusAttack += value.bonusAttack;
            BonusMP += value.bonusMP;
            BonusMPRegen += value.bonusMPRegen;
            BonusHP += value.bonusHP;
            BonusHPRegen += value.bonusHPRegen;
            BonusCriticalDamage += value.bonusCriticalDamage;
            BonusBossDamage += value.bonusBossDamage;
            BonusNormalDamage += value.bonusNormalDamage;
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

        while (CurrencyManager.Instance.TrySpendSlience(CurrencyType.Exp, RequiredExp))
        {
            levelKey++;

            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey, out var value);

            if (value == null)
            {
                break;
            }

            BonusAttack += value.bonusAttack;
            BonusMP += value.bonusMP;
            BonusMPRegen += value.bonusMPRegen;
            BonusHP += value.bonusHP;
            BonusHPRegen += value.bonusHPRegen;
            BonusCriticalDamage += value.bonusCriticalDamage;
            BonusBossDamage += value.bonusBossDamage;
            BonusNormalDamage += value.bonusNormalDamage;
            BonusCristal += value.bonusCristal;

            playerExpKey++;

            DataManager.Instance.PlayerLevelDict.TryGetValue(playerExpKey, out var playerExpTable);

            RequiredExp = playerExpTable.reqExp;

            CurrentLevel++;

            if (CurrentLevel > 30)
            {
                CurrencyManager.Instance.AddCurrency(CurrencyType.TraitPoint, 1);
            }

            OnLevelUp?.Invoke();
        }
    }
}
