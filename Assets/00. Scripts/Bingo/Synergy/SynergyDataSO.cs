using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SynergyDataSO", menuName = "Scriptable Objects/SynergyDataSO")]
public class SynergyDataSO : ScriptableObject
{
    [SerializeField] public SerializedDictionary<RarityType, SynergyItem> SynergyItems = new SerializedDictionary<RarityType, SynergyItem>();
    [SerializeField] public SerializedDictionary<SynergyStat, SerializedDictionary<RarityType, SynergyData>> SynergyDataDict = new SerializedDictionary<SynergyStat, SerializedDictionary<RarityType, SynergyData>>();
#if UNITY_EDITOR
    [ContextMenu("Generate Synergy Data")]
    public void Generate()
    {
        SynergyDataDict.Clear();

        foreach (SynergyStat stat in Enum.GetValues(typeof(SynergyStat)))
        {
            if (stat == SynergyStat.None)
                continue;
            
            var rarityDict = new SerializedDictionary<RarityType, SynergyData>();

            foreach (RarityType rarity in Enum.GetValues(typeof(RarityType)))
            {
                rarityDict.Add(rarity, new SynergyData());
                rarityDict[rarity].Init(stat);
            }
            
            SynergyDataDict.Add(stat, rarityDict);
        }
    }
#endif
}
