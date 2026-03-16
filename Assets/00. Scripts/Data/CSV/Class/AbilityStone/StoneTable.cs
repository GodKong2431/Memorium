using System;
using System.Collections.Generic;

[System.Serializable]
public class StoneTable : TableBase
{
    public StoneGrade stoneGrade;
    public StoneTier stoneTier;
    public int stoneUpCost;
    public int stoneUnlock;
    public int stoneNeedUp;
    public int stoneStatRerollCost;
    public int stoneUpResetCost;
    public float stoneUpStartProbability;
    public float stoneMaxProbability;
    public float stoneMinProbability;
    public int stoneFirstUpOpportunity;
    public int stoneSecondUpOpportunity;
    public int stoneThirdUpOpportunity;
}
