using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;


//1단계 : 아래 구조체를 grade별로 만든다
[Serializable]
public struct AbiltyStoneDictData
{
    public int stoneGrade;
    public AbilityStoneData abilityStoneData;

    public AbiltyStoneDictData(StoneGrade stoneGrade)
    {
        this.stoneGrade = (int)stoneGrade;
        abilityStoneData=new AbilityStoneData(false, null);
    }
}

//2단계 
[Serializable]
public struct AbilityStoneData
{
    public bool isUnlock;
    public List<AbilityStoneSlotData> slots;

    public AbilityStoneData(bool isUnlock = false, List<AbilityStoneSlotData> slots=null)
    {
        this.isUnlock = isUnlock;
        this.slots = slots;

        if (slots == null || slots.Count == 0)
        {
            this.slots = new List<AbilityStoneSlotData>();
            for (int i = 0; i < 3; i++)
            {
                AbilityStoneSlotData slot = new AbilityStoneSlotData();
                slot.statType = (int)StatType.None;
                slot.successCounter = new List<bool>();
                this.slots.Add(slot);
            }
        }
    }
}

[Serializable]
public struct AbilityStoneSlotData
{
    public int statType;
    public List<bool> successCounter;

}



