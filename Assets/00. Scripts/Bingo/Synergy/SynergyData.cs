using System;
using System.Collections;
using UnityEngine;

[System.Serializable]
public class SynergyData
{
    public RarityType rarityType;
    public StatType statType1;
    public StatType statType2;
    
    public float statUp1;
    public float StatUp2;
    
    public void LoadSynergy(int id)
    {
        DataManager.Instance.SynergyDict.TryGetValue(id, out var synergyTable);
        
        
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
    }
}
