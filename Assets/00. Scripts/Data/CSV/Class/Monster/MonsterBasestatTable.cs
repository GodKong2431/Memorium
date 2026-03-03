using System;
using System.Collections.Generic;

[System.Serializable]
public class MonsterBasestatTable : TableBase
{
    public string monsterName;
    public MonsterType monsterType;
    public long healthPoint;
    public long attackPoint;
    public float attackSpeed;
    public float speed;
    public float attackRange;
    public string animation;
    public string effect;
}
