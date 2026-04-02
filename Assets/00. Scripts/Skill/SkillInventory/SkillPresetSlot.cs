using System;

[System.Serializable]
public class SkillPresetSlot
{
    public const int EmptySkillId = -1;
    private const int DefaultM5JemSlotCount = 2;

    public int skillID = EmptySkillId;
    public int[] m5JemIDs = { EmptySkillId, EmptySkillId };
    public int m4JemID = EmptySkillId;

    public SkillPresetSlot()
    {
        Clear();
    }

    public SkillPresetSlot(int skillID, int[] m5JemIDs, int m4JemID)
    {
        this.skillID = skillID;
        this.m5JemIDs = m5JemIDs;
        this.m4JemID = m4JemID;
        Normalize();
    }

    public bool IsEmpty
    {
        get { return skillID <= 0; }
    }
    public void Normalize()
    {
        EnsureGemSlots();

        if (IsEmpty)
        {
            Clear();
            return;
        }

        for (int i = 0; i < m5JemIDs.Length; i++)
        {
            if (m5JemIDs[i] <= 0)
                m5JemIDs[i] = EmptySkillId;
        }

        if (m4JemID <= 0)
            m4JemID = EmptySkillId;
    }

    public SkillPresetSlot Clone()
    {
        EnsureGemSlots();
        return new SkillPresetSlot(skillID, (int[])m5JemIDs.Clone(), m4JemID);
    }

    public void Clear()
    {
        EnsureGemSlots();
        skillID = EmptySkillId;
        m5JemIDs[0] = EmptySkillId;
        m5JemIDs[1] = EmptySkillId;
        m4JemID = EmptySkillId;
    }

    private void EnsureGemSlots()
    {
        if (m5JemIDs != null && m5JemIDs.Length >= DefaultM5JemSlotCount)
            return;

        int[] normalizedGemIds = { EmptySkillId, EmptySkillId };
        if (m5JemIDs != null)
        {
            int copyCount = m5JemIDs.Length < DefaultM5JemSlotCount ? m5JemIDs.Length : DefaultM5JemSlotCount;
            for (int i = 0; i < copyCount; i++)
                normalizedGemIds[i] = m5JemIDs[i] > 0 ? m5JemIDs[i] : EmptySkillId;
        }

        m5JemIDs = normalizedGemIds;
    }
}
