using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SaveAbilityStoneData:ISaveData
{

    public List<AbiltyStoneDictData> abiltyStoneDictDatas;

    //변경 여부 체크
    private bool isDirty = false;
    public bool IsDirty => isDirty;

    public SaveAbilityStoneData()
    { }

    public void InitAblityStoneData()
    {
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
        if (abiltyStoneDictDatas.Count <= 0)
            InitAblityStoneData();
        foreach (AbiltyStoneDictData stoneDictData in abiltyStoneDictDatas)
        {
            //먼저 is unlock값 불러온다
            so.AbilityStoneDict[(StoneGrade)stoneDictData.stoneGrade]
                .isUnlock = stoneDictData.abilityStoneData.isUnlock;

            List<AbilityStoneSlot> abilityStoneSlots = new List<AbilityStoneSlot>();
            if (stoneDictData.abilityStoneData.slots != null)
            {
                foreach (AbilityStoneSlotData slotData in stoneDictData.abilityStoneData.slots)
                {
                    //어빌리티 스톤 만든다
                    AbilityStoneSlot slot = new AbilityStoneSlot((StatType)slotData.statType, slotData.successCounter);
                    abilityStoneSlots.Add(slot);
                }
                //어빌리티 스톤 리스트 자체를 전달하라
                so.AbilityStoneDict[(StoneGrade)stoneDictData.stoneGrade]
                    .Slots = abilityStoneSlots;
            }
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
