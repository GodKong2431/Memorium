using System;
using System.Collections.Generic;

[System.Serializable]
public class FairyGradeTable : TableBase
{
    public FairyGrade fairyGrade;
    public int maxLevel;
    public int costBase;
    public float costSlope;
    public int fragmentCostBase;
    public float fragmentCostSlope;
    public string auraEffectPrefabPath;
}
