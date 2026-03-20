using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
//스킬의 아이디 별, 그리고 종류별로 저장
public struct SkillInfoData
{
    public int skillId;
    public int skillLevel;
    public List<SkillGradeData> gradeData;

    public SkillInfoData(int skillId, int skillLevel=0)
    {
        this.skillId = skillId;
        this.skillLevel = skillLevel;
        gradeData = null;
    }

}

[Serializable]
public struct SkillGradeData
{
    public int grade;
    public int count;

    public SkillGradeData(SkillGrade grade)
    {
        this.grade = (int)grade;
        count = 0;
    }

    public void SetCount(int count)
    {
        this.count = count;
    }

}

[Serializable]
public class SkillPresetData
{
    public List<SkillPresetSlotData> skillPresetSlotData;

    public SkillPresetData()
    {
        skillPresetSlotData = new List<SkillPresetSlotData>();
    }
}

[Serializable]
public class SkillPresetSlotData
{
    public int skillId;
    public List<int> m5JemIDs;
    public int m4JemID;

    public SkillPresetSlotData()
    {
        skillId = -1;
        m5JemIDs = new List<int>();
        m4JemID = -1;
    }

    public SkillPresetSlotData(int m5JemIdCount)
    {
        skillId = -1;
        m5JemIDs= new List<int>();
        for (int i = 0; i < m5JemIdCount; i++)
            m5JemIDs.Add(-1);
        m4JemID = -1;
    }
}
