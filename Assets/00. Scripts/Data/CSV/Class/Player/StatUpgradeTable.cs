using System;
using System.Collections.Generic;

[System.Serializable]
public class StatUpgradeTable : TableBase
{
    public string statName;
    public float statInCrease;
    public float baseCost;
    public float costMultiplyRate;
}
