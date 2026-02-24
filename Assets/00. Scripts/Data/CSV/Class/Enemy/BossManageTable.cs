using System;
using System.Collections.Generic;

[System.Serializable]
public class BossManageTable : TableBase
{
    public AttackType attackType;
    public float skillDamageRate;
    public float castingTime;
    public int normalAtkCastRate;
    public int skillAtk1CastRate;
    public int skillAtk2CastRate;
    public string animation;
    public string effect;
}
