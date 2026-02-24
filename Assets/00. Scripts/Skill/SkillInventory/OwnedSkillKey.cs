using System;

public struct OwnedSkillKey : IEquatable<OwnedSkillKey>
{
    public int skillID;
    public SkillGrade grade;

    public OwnedSkillKey(int id, SkillGrade g)
    {
        skillID = id;
        grade = g;
    }

    public override int GetHashCode()
    {
        return (skillID, grade).GetHashCode();
    }

    public override bool Equals(object obj)
    {
        return obj is OwnedSkillKey other && Equals(other);
    }

    public bool Equals(OwnedSkillKey other)
    {
        return skillID == other.skillID && grade == other.grade;
    }
}