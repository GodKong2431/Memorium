using System.Collections.Generic;

public enum ClassType
{
    Assassin = 0,
    Warrior = 1,
    Magician = 2
}

public enum StatType
{
    None,
    HP = 1, HP_REGEN = 2,
    MP = 3, MP_REGEN = 4,
    ATK = 5, ATK_SPEED = 6,
    PHYS_DEF = 7, MAGIC_DEF = 8,
    CRIT_CHANCE = 9, CRIT_MULT = 10,
    MOVE_SPEED = 11,
    COOLDOWN_REDUCE = 12,
    GOLD_GAIN = 13, EXP_GAIN = 14,
    BOSS_DMG = 15, NORMAL_DMG = 16,
    DMG_MULT =17,
}

public enum SlotType
{
    Skill1, Skill2, Skill3,
    Weapon, Helmet, Armor, Glove, Boots,
    Fairy
}

public static class StatGroups
{
    public static readonly HashSet<StatType> MultTypes = new HashSet<StatType>
    {
        StatType.CRIT_CHANCE,
        StatType.CRIT_MULT,
        StatType.NORMAL_DMG,
        StatType.BOSS_DMG,
        StatType.DMG_MULT,
        StatType.EXP_GAIN,
        StatType.GOLD_GAIN,
        StatType.COOLDOWN_REDUCE,
    };
}

