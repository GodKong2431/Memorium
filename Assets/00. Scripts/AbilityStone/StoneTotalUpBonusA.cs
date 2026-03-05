using UnityEngine.Rendering.Universal;

[System.Serializable]
public class StoneTotalUpBonusA
{
    private int id;
    public StatType statType;
    public int totalUpCount;
    public string statName;
    public float increaseStat;
    
    public bool isUnlock;
    
    public void LoadStone(StatType statType)
    {
        id = AbilityStoneManager.ID;
        AbilityStoneManager.ID++;
        
        DataManager.Instance.StoneTotalUpBonusDict.TryGetValue(id,out var table);
        
        this.statType = statType;
        totalUpCount = table.stoneTotalUP;
        statName = table.stoneBonusStatName;
        increaseStat = table.stoneBonusStat;
    }
    
    public void Unlock(bool unlockState)
    {
        if (isUnlock == unlockState)
        {
            return;
        }
        
        isUnlock = unlockState;
        CharacterStatManager.Instance.FinalStat(statType);
    }
}
