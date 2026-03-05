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
    HP, HP_REGEN,
    MP, MP_REGEN,
    ATK, ATK_SPEED,
    PHYS_DEF, MAGIC_DEF,
    CRIT_CHANCE, CRIT_MULT,
    MOVE_SPEED,
    COOLDOWN_REDUCE,
    GOLD_GAIN, EXP_GAIN,
    BOSS_DMG, NORMAL_DMG,
    DMG_MULT
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

