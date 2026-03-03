using System;
using System.Collections.Generic;

[System.Serializable]
public class FairyGradeTable : TableBase
{
    public string gradeName;
    public int maxLevel;
    public int costBase;
    public float costSlope;
}
