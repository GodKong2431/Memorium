using System.Collections.Generic;

public enum StoneGrade
{
    Nomal = 0,
    Rare = 1,
    Unique = 2,
    Legendy = 3,
    Myth = 4
    
}

public static class StoneGroups
{
    public static readonly HashSet<PlayerStatType> StoneType = new HashSet<PlayerStatType>
    {
        PlayerStatType.ATK,
        PlayerStatType.MP,
        PlayerStatType.MP_REGEN,
        PlayerStatType.CRIT_CHANCE,
        PlayerStatType.CRIT_MULT,
        PlayerStatType.BOSS_DMG,
        PlayerStatType.NORMAL_DMG,
        PlayerStatType.DMG_MULT,
        PlayerStatType.HP,
        PlayerStatType.HP_REGEN,
        PlayerStatType.PHYS_DEF,
        PlayerStatType.MAGIC_DEF,
        PlayerStatType.GOLD_GAIN,
        PlayerStatType.EXP_GAIN,
    };
}