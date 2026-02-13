using System;
using System.Collections.Generic;

[System.Serializable]
public class PassiveInfoTable : TableBase
{
    public string skillNameKey;
    public StatType statType;
    public string iconPath;
    public bool isPercent;
    public float maxValue;
}
