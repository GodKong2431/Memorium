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

    public OwnedPixieData(PixieSaveData save, FairyInfoTable fairyTable = null, FairyGradeTable gradeTable = null)
    {
        pixieId = save.pixieId;
        level = save.level;
        if (fairyTable == null || gradeTable == null)
            TryGetData();
    }

    public OwnedPixieData(int pixieId, int level, FairyInfoTable fairyTable=null, FairyGradeTable gradeTable = null)
    {
        this.pixieId = pixieId;
        this.level = level;
        this.fairyTable = fairyTable;
        this.gradeTable = gradeTable;
        if (fairyTable == null || gradeTable==null) 
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
    public int GetFragmentCost()
    {
        if (gradeTable == null)
            return 0;

        double levelOffset = Math.Max(0, level - 1);
        double rawCost = gradeTable.fragmentCostBase + (levelOffset * gradeTable.fragmentCostSlope);
        return Math.Max(0, (int)Math.Ceiling(rawCost));
    }

    public void ExecuteLevelUp() => level++;
}

