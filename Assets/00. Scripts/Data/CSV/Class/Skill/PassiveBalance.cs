using System;
using System.Collections.Generic;

[System.Serializable]
public class PassiveBalance : TableBase
{
    public int skillID;
    public int grade;
    public float baseValue;
    public int growthID;
    public int promotionItemID;
    public int promotionCount;
}
