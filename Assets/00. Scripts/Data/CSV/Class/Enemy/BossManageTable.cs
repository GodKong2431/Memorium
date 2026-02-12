using System;
using System.Collections.Generic;

[System.Serializable]
public class BossManageTable : TableBase
{
    public AttackType attackType;
    public float skillDamageRate;
    public float castingTime;
    public int attackCastRate;
    public string animation;
    public string effect;
}
