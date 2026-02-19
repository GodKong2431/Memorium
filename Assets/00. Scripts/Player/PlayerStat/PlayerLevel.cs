using UnityEngine;

[System.Serializable]

public class PlayerLevel
{
    private int levelKey = 1020001;
    private int playerExpKey = 1510000;

    public int CurrentLevel;

    public BigDouble RequiredExp;

    public BigDouble CurrentExp;

    public int BonusAttack;
    public int BonusMP;
    public int BonusMPRegen;
    public int BonusHP;
    public int BonusHPRegen;
    public float BonusCriticalDamage;
    public float BonusBossDamage;
    public float BonusNormalDamage;
    public int BonusCristal;

    public PlayerLevel(int level)
    {
        for (int i = 0; i < level; i++)
        {
            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey++, out var value);

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

        DataManager.Instance.PlayerLevelDict.TryGetValue(playerExpKey, out var playerExpTable);

        RequiredExp = playerExpTable.reqExp;
    }

    public void ExpCheck()
    {
        if (CurrentExp >= RequiredExp)
        {
            DataManager.Instance.LevelbonusDict.TryGetValue(levelKey++, out var value);

            BonusAttack += value.bonusAttack;
            BonusMP += value.bonusMP;
            BonusMPRegen += value.bonusMPRegen;
            BonusHP += value.bonusHP;
            BonusHPRegen += value.bonusHPRegen;
            BonusCriticalDamage += value.bonusCriticalDamage;
            BonusBossDamage += value.bonusBossDamage;
            BonusNormalDamage += value.bonusNormalDamage;
            BonusCristal += value.bonusCristal;

            CurrentExp -= RequiredExp;

            DataManager.Instance.PlayerLevelDict.TryGetValue(playerExpKey++, out var playerExpTable);

            RequiredExp = playerExpTable.reqExp;

            CurrentLevel++;
        }
    }
}
