
using System;

[Serializable]
public struct PassiveSaveData
{
    public int skillId;
    public int grade;
    public int level;

    public PassiveSaveData(int skillId, int grade, int level)
    {
        this.skillId = skillId;
        this.grade = grade;
        this.level = level;
    }
}

public class OwnedPassiveData
{
    public int skillId;
    public int grade;
    public int level;

    public PassiveInfoTable passiveInfoTable;
    public PassiveGradeTable passiveGradeTable;

    public OwnedPassiveData(PassiveSaveData save)
    {
        skillId = save.skillId;
        grade = save.grade;
        level = save.level;
        TryGetData();
    }

    public OwnedPassiveData(int skillId, int grade, int level)
    {
        this.skillId = skillId;
        this.grade = grade;
        this.level = level;
        TryGetData();
    }

    public void TryGetData()
    {
        if (DataManager.Instance.PassiveInfoDict.TryGetValue(skillId, out var _infoTable))
            passiveInfoTable = _infoTable;

        int gradeID = 4140000 + grade;
        if (DataManager.Instance.PassiveGradeDict.TryGetValue(gradeID, out var _gradeTable))
            passiveGradeTable = _gradeTable;
    }

    public bool IsMaxLevel() => passiveGradeTable != null && level >= passiveGradeTable.maxLevel;
    public bool IsMaxGrade() => grade >= 5;
    public bool CanLevelUp() => !IsMaxLevel();
    public bool CanEvolve() => IsMaxLevel() && !IsMaxGrade();

    /// <summary>
    /// 레벨업 골드 비용: lvCostBase * (1 + lvCostSlope * level)
    /// </summary>
    public BigDouble GetLevelUpCost()
    {
        if (passiveGradeTable == null) return BigDouble.Zero;
        return passiveGradeTable.lvCostBase * (1f + passiveGradeTable.lvCostSlope * level);
    }
    /// <summary>
    /// 승급에 필요한 주문서 수량
    /// </summary>
    public int GetEvolveScrollCount()
    {
        if (passiveGradeTable == null) return 0;
        return passiveGradeTable.reqPromCount;
    }

    /// <summary>
    /// 현재 등급에 맞는 승급 주문서 아이템 ID 반환
    /// </summary>
    public int GetEvolveItemId()
    {
        if (passiveInfoTable == null) return 0;

        return grade switch
        {
            1 => passiveInfoTable.grd1ReqItem,
            2 => passiveInfoTable.grd2ReqItem,
            3 => passiveInfoTable.grd3ReqItem,
            4 => passiveInfoTable.grd4ReqItem,
            _ => 0,
        };
    }

    /// <summary>
    /// 현재 등급/레벨 기준 스탯 값: Base + LvInc * level
    /// </summary>
    public float GetCurrentStatValue()
    {
        if (passiveInfoTable == null) return 0f;
        return GetBaseByGrade() + GetLvIncByGrade() * level;
    }

    public StatType GetStatType()
    {
        if (passiveInfoTable == null) return 0;
        return passiveInfoTable.statType;
    }

    public bool IsPercent()
    {
        return passiveInfoTable != null && passiveInfoTable.isPercent;
    }

    public float GetMaxValue()
    {
        if (passiveInfoTable == null) return 0f;
        return passiveInfoTable.maxValue;
    }

    public void ExecuteLevelUp()
    {
        level++;
    }

    public void ExecuteEvolve()
    {
        grade++;
        level = 0;
        TryGetData();
    }

    private float GetBaseByGrade()
    {
        return grade switch
        {
            1 => passiveInfoTable.grd1Base,
            2 => passiveInfoTable.grd2Base,
            3 => passiveInfoTable.grd3Base,
            4 => passiveInfoTable.grd4Base,
            5 => passiveInfoTable.grd5Base,
            _ => 0f,
        };
    }

    private float GetLvIncByGrade()
    {
        return grade switch
        {
            1 => passiveInfoTable.grd1LvInc,
            2 => passiveInfoTable.grd2LvInc,
            3 => passiveInfoTable.grd3LvInc,
            4 => passiveInfoTable.grd4LvInc,
            5 => passiveInfoTable.grd5LvInc,
            _ => 0f,
        };
    }
}