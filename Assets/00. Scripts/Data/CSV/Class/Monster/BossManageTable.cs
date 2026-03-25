using System;
using System.Collections.Generic;

[System.Serializable]
public class BossManageTable : TableBase
{
    public AttackType attackType;
    public AtkAttributeType atkAttributeType;
    public float skillDamageRate;
    public float castingDelay;
    public float castingTime;
    public int baseAtkCastRate;
    public int atkBiasIncreaseRate;
    public float atkCoolTime;
    public bool resetBiasCheck;
    public string rangeShape;
    public float rangeSize;
    public string colorNormal;
    public string colorFillUp;
    public string animation;
    public string effect;
}
