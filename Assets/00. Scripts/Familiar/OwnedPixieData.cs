using System;

[Serializable]
public struct PixieSaveData
{
    public int pixieId;
    public int level;

    public PixieSaveData(int id, int _level)
    {
        pixieId = id;
        level = _level;
    }
}



[Serializable]
public class OwnedPixieData
{
    public int pixieId;
    public int level;

    public FairyInfoTable fairyTable;
    public FairyGradeTable gradeTable;

    public OwnedPixieData(PixieSaveData save)
    {
        pixieId = save.pixieId;
        level = save.level;
        TryGetData();
    }

    public void TryGetData()
    {
        if (DataManager.Instance.FairyInfoDict.TryGetValue(pixieId, out var _fairyTable)) 
        if (_fairyTable != null) fairyTable = _fairyTable;
        if (fairyTable != null && DataManager.Instance.FairyGradeDict.TryGetValue(this.fairyTable.gradeID, out var _gradeTable))
        if (_gradeTable != null) gradeTable = _gradeTable;

    }
    public bool IsMaxLevel() => level >= gradeTable.maxLevel;
    public bool CanEvolve() => IsMaxLevel() && fairyTable.nextID != 0;
    public bool CanLevelUp() => !IsMaxLevel();
    public BigDouble GetLevelUpCost() => gradeTable.costBase + (1f + (level - 1f) * gradeTable.costSlope);

    /// <summary>
    /// 신화등급 한정이고, 나머지 등급에선 조각 사용 코스트 없습니다
    /// </summary>
    /// <returns></returns>
    public float GetFragmentCost() => gradeTable.fragmentCostBase + (1f + (level - 1f) * gradeTable.fragmentCostSlope);

    public void ExecuteLevelUp() => level++;
}

