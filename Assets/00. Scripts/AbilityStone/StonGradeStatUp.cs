using System;

[Serializable]
public class StoneGradeStatUp
{
    private int id;
    public string statName;
    public StatType statType;
    
    private StoneGradeStatUpTable table;
    
    public void LoadStone(StatType statType)
    {
        id = AbilityStoneManager.ID;
        AbilityStoneManager.ID++;
        
        DataManager.Instance.StoneGradeStatUpDict.TryGetValue(id, out table);
        
        this.statType = statType;
        
        statName = table.stoneGradeStatName;
    }
    
    public float SetStat(StoneGrade stoneGrade)
    {
        return stoneGrade switch
        {
            StoneGrade.Normal => table.stoneNormal,
            StoneGrade.Rare => table.stoneUnique,
            StoneGrade.Unique => table.stoneUnique,
            StoneGrade.Legendy => table.stoneLegend,
            StoneGrade.Myth => table.stoneMyth,
            _ => throw new ArgumentOutOfRangeException(nameof(stoneGrade), "범위 벗아남")
        };
    }
}
