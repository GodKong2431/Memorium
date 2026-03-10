using System;
using System.Collections.Generic;

[System.Serializable]
public class PassiveGradeTable : TableBase
{
    public string gradeName;
    public int maxLevel;
    public int lvCostBase;
    public float lvCostSlope;
    public int reqPromCount;
}
