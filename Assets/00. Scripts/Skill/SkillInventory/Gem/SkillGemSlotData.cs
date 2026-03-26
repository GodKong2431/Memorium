using System.Collections.Generic;

[System.Serializable]
public class SkillGemSlotData
{
    public int skillID;
    public int[] m5JemIDs = { -1, -1 };
    public int m4JemID = -1;

    public SkillGemSlotData(int skillID)
    {
        this.skillID = skillID;
    }

    public SkillGemSlotData Clone()
    {
        var clone = new SkillGemSlotData(skillID);
        clone.m5JemIDs = new int[] { m5JemIDs[0], m5JemIDs[1] };
        clone.m4JemID = m4JemID;
        return clone;
    }
}

[System.Serializable]
public class SkillGemPreset
{
    // skillID → 해당 스킬의 젬 장착 정보
    private Dictionary<int, SkillGemSlotData> gemDataBySkillId = new Dictionary<int, SkillGemSlotData>();

    public SkillGemSlotData GetOrCreate(int skillId)
    {
        if (!gemDataBySkillId.TryGetValue(skillId, out var data))
        {
            data = new SkillGemSlotData(skillId);
            gemDataBySkillId[skillId] = data;
        }
        return data;
    }

    public SkillGemSlotData Get(int skillId)
    {
        gemDataBySkillId.TryGetValue(skillId, out var data);
        return data;
    }

    public void Set(int skillId, SkillGemSlotData data)
    {
        gemDataBySkillId[skillId] = data;
    }

    public Dictionary<int, SkillGemSlotData> GetAll() => gemDataBySkillId;
}