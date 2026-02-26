
[System.Serializable]
public class SkillPresetSlot
{
    public int skillID;
    public int[] m5JemIDs;
    public int m4JemID;
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