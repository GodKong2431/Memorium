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
        TryGetTables();
    }

    public void TryGetTables()
    {
        if (DataManager.Instance.FairyInfoDict.TryGetValue(pixieId, out var _fairyTable)) fairyTable = _fairyTable;
        if (fairyTable != null && DataManager.Instance.FairyGradeDict.TryGetValue(this.fairyTable.gradeID, out var _gradeTable)) gradeTable = _gradeTable;
    }
    public bool CanEvolve() => level >= gradeTable.maxLevel;
    public BigDouble GetLevelUpCost() => gradeTable.costBase + (1f + (level - 1f) * gradeTable.costSlope);
    public float GetFragmentCost() => gradeTable.fragmentCostBase + (1f + (level - 1f) * gradeTable.fragmentCostSlope);
    public bool IsMaxLevel => level >= gradeTable.maxLevel;
    public void ExecuteLevelUp() => level++;
}

