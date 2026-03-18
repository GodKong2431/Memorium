
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
    //m4 애드온 발동 횟수/ 현재는 한번만 이지만 추후 여러번 발동하는 애드온이 나올상황을 대비해서 만들어둠
    private int addonTriggerCount = 0;
    public int GetAddonTriggerCount() => addonTriggerCount;

    [Header("M5: 상태 이상")]
    public SkillModule5Table m5DataA;
    public SkillModule5Table m5DataB;

    public SkillDataContext(int skillID, int m4ID = -1, int m5IDa = -1, int m5IDb = -1)
    {
        SetSkillContext(skillID, m4ID, m5IDa, m5IDb);
    }
    public void SetSkillContext(int skillID, int m4ID = -1, int m5IDa = -1, int m5IDb = -1)
    {
        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillID, out var table))
        {
            skillData = null;
            m4Data = null;
            m5DataA = null;
            m5DataB = null;
            return;
        }
        if (skillData == null) skillData = new SkillData();
        skillData.skillTable = table;
        skillData.m1Data = DataManager.Instance.SkillModule1Dict.GetValueOrDefault(table.m1ID);
        skillData.m2Data = DataManager.Instance.SkillModule2Dict.GetValueOrDefault(table.m2ID);
        skillData.m3Data = DataManager.Instance.SkillModule3Dict.GetValueOrDefault(table.m3ID);

        m4Data = DataManager.Instance.SkillModule4Dict.GetValueOrDefault(m4ID);
        m5DataA = DataManager.Instance.SkillModule5Dict.GetValueOrDefault(m5IDa);
        m5DataB = DataManager.Instance.SkillModule5Dict.GetValueOrDefault(m5IDb);
    }
    public void RecordAddonTrigger()
    {
        addonTriggerCount++;
    }
    public void ResetAddonState()
    {
        addonTriggerCount = 0;
    }
}
