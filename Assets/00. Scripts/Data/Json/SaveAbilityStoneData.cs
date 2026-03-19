using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveAbilityStoneData:ISaveData
{

    public List<AbiltyStoneDictData> abiltyStoneDictDatas;

    public bool onCBT = false;

    //변경 여부 체크
    private bool isDirty = false;
    public bool IsDirty => isDirty;

    public SaveAbilityStoneData()
    { }

    public void InitAblityStoneData()
    {
        if (!onCBT)
        {
            abiltyStoneDictDatas = null;
            onCBT = true;
        }


        if (abiltyStoneDictDatas == null||abiltyStoneDictDatas.Count <= 0)
        {
            abiltyStoneDictDatas=new List<AbiltyStoneDictData>();
            foreach (StoneGrade grade in Enum.GetValues(typeof(StoneGrade)))
            {
                abiltyStoneDictDatas.Add(new AbiltyStoneDictData(grade));
            }
        }
    }

    public void LoadAbilityStoneData(AbilityStoneSO so)
    {
        if (abiltyStoneDictDatas == null || abiltyStoneDictDatas.Count <= 0)
            InitAblityStoneData();
        foreach (AbiltyStoneDictData stoneDictData in abiltyStoneDictDatas)
        {
            if (!so.AbilityStoneDict.TryGetValue((StoneGrade)stoneDictData.stoneGrade, out AbilityStone stone)
                || stone == null)
            {
                continue;
            }

            List<AbilityStoneSlot> abilityStoneSlots = new List<AbilityStoneSlot>();
            if (stoneDictData.abilityStoneData.slots != null)
            {
                foreach (AbilityStoneSlotData slotData in stoneDictData.abilityStoneData.slots)
                {
                    AbilityStoneSlot slot = new AbilityStoneSlot((StatType)slotData.statType, slotData.successCounter);
                    abilityStoneSlots.Add(slot);
                }
            }

            stone.isUnlock = stoneDictData.abilityStoneData.isUnlock;
            stone.RestoreLoadedSlots(abilityStoneSlots);
        }
    }

    public void SaveAbilityStoneDataBySO(AbilityStoneSO so)
    {
        foreach (AbilityStone abilityStone in so.AbilityStoneDict.Values)
        {
            //해당 스톤 그레이드의 인덱스를 찾아라
            int index = abiltyStoneDictDatas.FindIndex
                (x => x.stoneGrade == (int)abilityStone.StoneGrade);
            if (index == -1)
            {
                abiltyStoneDictDatas.Add(new AbiltyStoneDictData(abilityStone.StoneGrade));
                index = abiltyStoneDictDatas.Count - 1;
                Debug.Log("[SaveAbilityStoneData]  새로 만든다");
            }

            AbiltyStoneDictData stoneDictData = abiltyStoneDictDatas[index];
            AbilityStoneData stoneData = stoneDictData.abilityStoneData;
            stoneData.isUnlock = abilityStone.isUnlock;

           
            if (stoneData.slots == null)
            {
                Debug.Log("[SaveAbilityStoneData] 슬롯이 없다");
            }
            for (int i = 0; i < stoneData.slots.Count; i++)
            {
                AbilityStoneSlotData slotData = stoneData.slots[i];
                slotData.statType = (int)abilityStone.Slots[i].SlotType;
                slotData.successCounter = abilityStone.Slots[i].successCounter;
                stoneData.slots[i]=slotData;
            }
            stoneDictData.abilityStoneData = stoneData;

            abiltyStoneDictDatas[index] = stoneDictData;
        }

        isDirty= true;
    }


    public void ClearDirty()
    {
        isDirty = false;
    }
}
