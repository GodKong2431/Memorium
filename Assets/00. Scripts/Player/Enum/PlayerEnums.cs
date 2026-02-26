using System.Collections.Generic;

public enum ClassType
{
    Assassin = 0,
    Warrior = 1,
    Magician = 2
}

public enum StatType
{
    HP, HPRegen,
    ATK, ATKSpeed,
    DEF, MagicDEF,
    MP, MPRegen,
    CritChance, CritMult,
    CoolDown,
    MoveSpeed,
    ExpGain, GoldGain
}

public enum SlotType
{
    Skill1, Skill2, Skill3,
    Weapon, Helmet, Armor, Glove, Boots,
    Fairy
}

public static class StatGroups
{
    public static readonly HashSet<PlayerStatType> MultTypes = new HashSet<PlayerStatType>
    {
        PlayerStatType.CRIT_CHANCE,
        PlayerStatType.CRIT_MULT,
        PlayerStatType.NORMAL_DMG,
        PlayerStatType.BOSS_DMG,
        PlayerStatType.DMG_MULT,
        PlayerStatType.EXP_GAIN,
        PlayerStatType.GOLD_GAIN,
        PlayerStatType.COOLDOWN_REDUCE,
    };
}


