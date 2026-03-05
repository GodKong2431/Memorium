using System;
using System.Collections.Generic;

[System.Serializable]
public class FairyStatTable : TableBase
{
    public StatType statType;
    public float baseValue;
    public float lvGrowth;
    public float grdGrowth;
    public float mythicLvGrowth;
}
