using System;
using System.Collections.Generic;

[System.Serializable]
public class EquipStatsTable : TableBase
{
    public StatType statType;
    public float statPerLevel;
    public int maxLevel;
    public BigDouble baseCost;
    public float costPerLevel;
    public float costPerTier;
    public float bonusStatPerLevel;
    public float baseBonusStat;
    public float bonusStatPerStep;
    public float bonusStatPerTier;
}
