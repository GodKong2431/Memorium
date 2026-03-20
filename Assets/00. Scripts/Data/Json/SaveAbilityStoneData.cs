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
        AbilityStoneSO so = AbilityStoneManager.Instance != null ? AbilityStoneManager.Instance.so : null;

        if (!onCBT)
        {
            abiltyStoneDictDatas = null;
            onCBT = true;
        }

        if (abiltyStoneDictDatas == null)
        {
            abiltyStoneDictDatas = new List<AbiltyStoneDictData>();
        }

        EnsureTierGradeEntries(so);
    }

    public void LoadAbilityStoneData(AbilityStoneSO so)
    {
        if (abiltyStoneDictDatas == null || abiltyStoneDictDatas.Count <= 0)
        {
            InitAblityStoneData();
        }

        EnsureTierGradeEntries(so);

        foreach (AbiltyStoneDictData stoneDictData in abiltyStoneDictDatas)
        {
            if (so == null || !so.AbilityStoneDict.TryGetValue(stoneDictData.tier, out var tierStoneDict))
            {
                continue;
            }

            if (!tierStoneDict.TryGetValue((StoneGrade)stoneDictData.stoneGrade, out AbilityStone stone)
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
        if (so == null)
        {
            return;
        }

        if (abiltyStoneDictDatas == null)
        {
            abiltyStoneDictDatas = new List<AbiltyStoneDictData>();
        }

        EnsureTierGradeEntries(so);

        foreach (var tierEntry in so.AbilityStoneDict)
        {
            int tierKey = tierEntry.Key;
            foreach (AbilityStone abilityStone in tierEntry.Value.Values)
            {
                if (abilityStone == null)
                {
                    continue;
                }

                //해당 스톤 그레이드의 인덱스를 찾아라
                int index = abiltyStoneDictDatas.FindIndex
                    (x => x.tier == tierKey && x.stoneGrade == (int)abilityStone.StoneGrade);
                if (index == -1)
                {
                    abiltyStoneDictDatas.Add(new AbiltyStoneDictData(abilityStone.StoneGrade, tierKey));
                    index = abiltyStoneDictDatas.Count - 1;
                    Debug.Log("[SaveAbilityStoneData]  새로 만든다");
                }

                AbiltyStoneDictData stoneDictData = abiltyStoneDictDatas[index];
                AbilityStoneData stoneData = stoneDictData.abilityStoneData;
                stoneData.isUnlock = abilityStone.isUnlock;


                if (stoneData.slots == null || stoneData.slots.Count != abilityStone.Slots.Count)
                {
                    stoneData = new AbilityStoneData(stoneData.isUnlock, null);
                }

                int slotCount = Mathf.Min(stoneData.slots.Count, abilityStone.Slots.Count);
                for (int i = 0; i < slotCount; i++)
                {
                    AbilityStoneSlotData slotData = stoneData.slots[i];
                    slotData.statType = (int)abilityStone.Slots[i].SlotType;
                    slotData.successCounter = abilityStone.Slots[i].successCounter;
                    stoneData.slots[i] = slotData;
                }
                stoneDictData.abilityStoneData = stoneData;

                abiltyStoneDictDatas[index] = stoneDictData;
            }
        }
        isDirty = true;
    }


    public void ClearDirty()
    {
        isDirty = false;
    }

    private void EnsureTierGradeEntries(AbilityStoneSO so)
    {
        if (so == null || so.AbilityStoneDict == null)
        {
            return;
        }

        if (abiltyStoneDictDatas == null)
        {
            abiltyStoneDictDatas = new List<AbiltyStoneDictData>();
        }

        foreach (int tierKey in so.AbilityStoneDict.Keys)
        {
            foreach (StoneGrade grade in Enum.GetValues(typeof(StoneGrade)))
            {
                int index = abiltyStoneDictDatas.FindIndex(
                    x => x.tier == tierKey && x.stoneGrade == (int)grade);

                if (index == -1)
                {
                    abiltyStoneDictDatas.Add(new AbiltyStoneDictData(grade, tierKey));
                }
            }
        }
    }
}
