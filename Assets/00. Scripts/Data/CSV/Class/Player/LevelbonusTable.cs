using System;
using System.Collections.Generic;

[System.Serializable]
public class LevelbonusTable : TableBase
{
    public int level;
    public int bonusAttack;
    public int bonusMP;
    public int bonusMPRegen;
    public int bonusHP;
    public int bonusHPRegen;
    public float bonusCriticalDamage;
    public float bonusBossDamage;
    public float bonusNormalDamage;
    public int bonusCristal;
}
