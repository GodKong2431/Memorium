using System;
using System.Collections.Generic;

[System.Serializable]
public class CharacterBaseStatTable : TableBase
{
    public ClassType classType;
    public float baseAttack;
    public float baseAttackSpeed;
    public float baseHP;
    public float baseHPRegen;
    public float baseMP;
    public float baseMPRegen;
    public float baseCritical;
    public float baseCriticalMultiPlier;
    public float baseBossDamage;
    public float baseNormalDamage;
    public float baseFinalMultiPlier;
    public float basePhysicalResist;
    public float baseMagicResist;
    public float baseMoveSpeed;
    public float baseCooltimeRegen;
    public float baseMoneyGain;
    public float baseExpGain;
}

