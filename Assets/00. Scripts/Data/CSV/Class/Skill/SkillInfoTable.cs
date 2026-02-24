using System;
using System.Collections.Generic;

[System.Serializable]
public class SkillInfoTable : TableBase
{
    public int m1ID;
    public int m2ID;
    public int m3ID;
    public float manaCost;
    public float skillDamage;
    public float skillDamageValue;
    public string skillName;
    public string skillDesc;
    public float skillRange;
    public float skillCooldown;
    public SkillType skillType;
    public string skillVFX;
    public string skillSound;
    public string desc;
    public string skillIcon;
}
