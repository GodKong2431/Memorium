using System;
using System.Collections.Generic;

[System.Serializable]
public class StoneGradeStatUpTable : TableBase
{
    public string stoneGradeStatName;
    public float stoneNormal;
    public float stoneRare;
    public float stoneUnique;
    public float stoneLegend;
    public float stoneMyth;
    public bool stoneTier;
}
