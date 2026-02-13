using System;
using System.Collections.Generic;

[System.Serializable]
public class PassiveGrowthTable : TableBase
{
    public float growthValue;
    public int maxLevel;
    public int baseCost;
    public float costIncrease;
}
