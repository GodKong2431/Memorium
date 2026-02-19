using System;
using System.Collections.Generic;

[System.Serializable]
public class SkillModule5Table : TableBase
{
    public int skillID;
    public M5Type m5Type;
    public float damage;
    public ApplyType applyType;
    public string duration;
    public int maxStack;
    public int defDown;
    public string icon;
    public string sound;
    public string m5VFX;
    public string desc;
}