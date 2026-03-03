using UnityEngine;
using System;
using System.Data.Common;

[Serializable]
public class StoneStatProbability
{
    [SerializeField] private int id;    
    [SerializeField] private PlayerStatType stoneType;
    
    [SerializeField] private string statName;
    
    [SerializeField] private float firstSlotProbability;
    [SerializeField] private float secondSlotProbability;
    [SerializeField] private float thirdSlotProbability;

    public PlayerStatType StoneType {get {return stoneType;}}
    public string StatName {get {return statName;}}
    public float FirstSlotProbability {get {return firstSlotProbability;}}
    public float SecondSlotProbability {get {return secondSlotProbability;}}
    public float ThirdSlotProbability {get {return thirdSlotProbability;}}
    


    public void LoadStone()
    {
        id = Test1.ID;
        Test1.ID++;
        
        Debug.Log(id);
        
        DataManager.Instance.StoneStatProbabilityDict.TryGetValue(id, out StoneStatProbabilityTable Table);
        
        statName = Table.stoneRerollStatName;
        firstSlotProbability = Table.stoneFirstSlot;
        secondSlotProbability = Table.stoneSecondSlot;
        thirdSlotProbability = Table.stoneThridSlot;
        
    }
}
