using UnityEngine;
using System;

[Serializable]
public class StoneStatProbability
{
    [SerializeField] private int id;    
    [SerializeField] private StatType stoneType;
    
    [SerializeField] private string statName;
    
    [SerializeField] private float firstSlotProbability;
    [SerializeField] private float secondSlotProbability;
    [SerializeField] private float thirdSlotProbability;

    public StatType StoneType {get {return stoneType;}}
    public string StatName {get {return statName;}}
    public float FirstSlotProbability {get {return firstSlotProbability;}}
    public float SecondSlotProbability {get {return secondSlotProbability;}}
    public float ThirdSlotProbability {get {return thirdSlotProbability;}}
    


    public void LoadStone(StatType statType)
    {
        id = AbilityStoneManager.ID;
        AbilityStoneManager.ID++;
                
        DataManager.Instance.StoneStatProbabilityDict.TryGetValue(id, out StoneStatProbabilityTable Table);
        
        stoneType = statType;
        statName = Table.stoneRerollStatName;
        firstSlotProbability = Table.stoneFirstSlot;
        secondSlotProbability = Table.stoneSecondSlot;
        thirdSlotProbability = Table.stoneThridSlot;
        
    }
}
