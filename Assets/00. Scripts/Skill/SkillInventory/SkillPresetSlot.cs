
[System.Serializable]
public class SkillPresetSlot
{
    public int skillID=-1;
    public int[] m5JemIDs = {-1,-1 };
    public int m4JemID = -1;
    public SkillPresetSlot()
    {
        Clear();
    }

    public bool IsEmpty
    {
        get { return skillID == -1; }
    }

    public void Clear()
    {
        skillID = -1;
        m5JemIDs[0] = -1;
        m5JemIDs[1] = -1;
        m4JemID = -1;
    }
}