using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class SynergyData
{
    public int ID;
    public SynergyStat synergyStat;
    public RarityType rarityType;
    public StatType statType1;
    public StatType statType2;
    public int dustProvided;
    public int count;
    public float statUp1;
    public float StatUp2;
    
    public void LoadSynergy(int id)
    {
        ID = id;
        
        DataManager.Instance.SynergyDict.TryGetValue(id, out var synergyTable);
        
        statUp1 = synergyTable.statUp1;
        StatUp2 = synergyTable.statUp2;
        
        rarityType = synergyTable.synergyRarity - 1;
    }
    
    public void Init(SynergyStat synergyStat)
    {
        switch (synergyStat)
        {
            case SynergyStat.DEF:
                statType1 = StatType.PHYS_DEF;
                statType2 = StatType.MAGIC_DEF;
                break;

            default:
                if (Enum.TryParse<StatType>(synergyStat.ToString(), out var statType))
                {
                    statType1 = statType;
                }
                break;
        }

        this.synergyStat = synergyStat;
    }
}
