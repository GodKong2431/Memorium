using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
//스킬의 아이디 별, 그리고 종류별로 저장
public struct SkillInfoData
{
    public int skillId;
    public int skillLevel;
    public int skillGrade;
}

[Serializable]
public struct SkillPresetData
{
    public List<SkillPresetSlotData> skillPresetSlotData;
}

[Serializable]
public struct SkillPresetSlotData
{
    public int skillId;
    public List<int> m5JemIDs;
    public int m4JemID;

    public SkillPresetSlotData(int m5JemIdCount)
    {
        skillId = -1;
        m5JemIDs= new List<int>();
        for (int i = 0; i < m5JemIdCount; i++)
            m5JemIDs.Add(-1);
        m4JemID = -1;
    }
}
