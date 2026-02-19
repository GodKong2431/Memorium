
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SkillData
{
    public SkillInfoTable skillTable;

    [Header("M1: 이동")]
    public SkillModule1Table m1Data;

    [Header("M2: 범위")]
    public SkillModule2Table m2Data;

    [Header("M3: 실행 방식")]
    public SkillModule3Table m3Data;

}

[System.Serializable]
public class SkillDataContext
{
    [Header("기본 스킬 데이터")]
    public SkillData skillData;

    [Header("M4: 추가 효과")]
    public SkillModule4Table m4Data;

    [Header("M5: 상태 이상")]
    public SkillModule5Table m5Data;

    public void Init(int skillID, int m4ID = -1, int m5ID = -1)
    {
        if (!DataManager.Instance.DataLoad) return;
        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillID, out var table)) return;

        if (skillData == null) skillData = new SkillData();

        skillData.skillTable = table;
        skillData.m1Data = DataManager.Instance.SkillModule1Dict.GetValueOrDefault(table.m1ID);
        skillData.m2Data = DataManager.Instance.SkillModule2Dict.GetValueOrDefault(table.m2ID);
        skillData.m3Data = DataManager.Instance.SkillModule3Dict.GetValueOrDefault(table.m3ID);

        m4Data = DataManager.Instance.SkillModule4Dict.GetValueOrDefault(m4ID);
        m5Data = DataManager.Instance.SkillModule5Dict.GetValueOrDefault(m5ID);
    }

}