using System;
using System.Collections.Generic;

[System.Serializable]
public class PassiveSetTable : TableBase
{
    public int reqGrade;
    public int reqCount;
    public StatType effectType;
    public StatType effectType2;
    public float effectValue;
    public float effectValue2;
    public string vfxPath;
}
