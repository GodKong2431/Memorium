using System;
using System.Collections.Generic;

[System.Serializable]
public class PassiveInfoTable : TableBase
{
    public string skillNameKey;
    public StatType statType;
    public bool isPercent;
    public float maxValue;
    public string iconPath;
    public float grd1Base;
    public float grd1LvInc;
    public int grd1ReqItem;
    public float grd2Base;
    public float grd2LvInc;
    public int grd2ReqItem;
    public float grd3Base;
    public float grd3LvInc;
    public int grd3ReqItem;
    public float grd4Base;
    public float grd4LvInc;
    public int grd4ReqItem;
    public float grd5Base;
    public float grd5LvInc;
    public int grd5ReqItem;
}
