using System.Collections.Generic;

public enum StoneGrade
{
    Normal = 0,
    Rare = 1,
    Unique = 2,
    Legendy = 3,
    Myth = 4
    
}

public static class StoneGroups
{
    public static readonly HashSet<StatType> StoneType = new HashSet<StatType>
    {
        StatType.ATK,
        StatType.MP,
        StatType.MP_REGEN,
        StatType.CRIT_CHANCE,
        StatType.CRIT_MULT,
        StatType.BOSS_DMG,
        StatType.NORMAL_DMG,
        StatType.DMG_MULT,
        StatType.HP,
        StatType.HP_REGEN,
        StatType.PHYS_DEF,
        StatType.MAGIC_DEF,
        StatType.GOLD_GAIN,
        StatType.EXP_GAIN,
    };
}