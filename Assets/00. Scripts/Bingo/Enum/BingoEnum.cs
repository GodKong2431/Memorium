using UnityEngine;

public enum CellRarity
{
    Normal = 0,
    Rare = 1,
    Epic = 2,
    Legendary = 3,
    Myth = 4,
}

public enum SynergyDirection
{
    None,
    Row,
    Column,
    Diagonal
}

public enum BingoItemType
{
    Lock,
    Pluck,
    Recall,
    Again,
}

public enum SynergyStat
{
    None,
    HP = 1,
    ATK = 2,
    ATK_SPEED = 3,
    DEF = 4,
    MOVE_SPEED = 5,
    GOLD_GAIN = 6,
    BOSS_DMG = 7,
    NORMAL_DMG = 8,
}

public enum Direction
{
    Left,
    Right,
    Up,
    Down,
}