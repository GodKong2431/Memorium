
[System.Serializable]
public class SkillPresetSlot
{
    public OwnedSkillKey skillKey;
    public int[] m5JemIDs = { -1, -1 };  
    public int m4JemID = -1;              

    public SkillPresetSlot()
    {
        skillKey = new OwnedSkillKey(-1, SkillGrade.Fragment);
        m5JemIDs = new int[] { -1, -1 };
        m4JemID = -1;
    }

    public bool IsEmpty
    {
        get { return skillKey.skillID == -1; }
    }

    public void Clear()
    {
        skillKey = new OwnedSkillKey(-1, SkillGrade.Fragment);
        m5JemIDs[0] = -1;
        m5JemIDs[1] = -1;
        m4JemID = -1;
    }
}