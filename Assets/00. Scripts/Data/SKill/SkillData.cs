using UnityEngine;

public enum SkillType
{
    Normal,
    Ultimate,
    Count,
}

[System.Serializable]
public class SkillData
{
    private int skillID;
    private int m1ID;
    private int m2ID;
    private int m3ID;
    private int m4ID;
    private float skillDamage;
    private float skillDamegeValue;
    private string skillName;
    private string skillDesc;
    private string skillIcon;
    private float skillCooldown;
    private SkillType skillType;
    private string skillSound;
    private string desc;

    public int SkillID { get { return skillID; }

}
