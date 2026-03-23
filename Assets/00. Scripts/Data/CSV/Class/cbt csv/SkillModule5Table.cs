using System;
using System.Collections.Generic;

[System.Serializable]
public class SkillModule5Table : TableBase
{
    public int m5ItemID;
    public int skillID;
    public M5Type m5Type;
    public float tickInterval;
    public ApplyType applyType;
    public float duration;
    public int maxStack;
    public float defDown;
    public string icon;
    public string sound;
    public string m5VFX;
    public string m5VFX2;
    public string desc;
}
