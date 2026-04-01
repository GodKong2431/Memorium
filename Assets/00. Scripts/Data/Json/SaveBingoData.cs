using AYellowpaper.SerializedCollections;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[Serializable]
public class SaveBingoData : ISaveData
{
    //현재는 하나의 빙고만 존재함
    public int bingoCount = 1;
    public List<BingoBoardSaveData> bingoData;
    private bool isDirty = false;
    public bool IsDirty => isDirty;

    public SaveBingoData() { }

    public bool InitBingoData()
    {
        bool loadData = true;
        if (bingoData == null)
        {
            bingoData = new List<BingoBoardSaveData>();
            loadData = false;
        }
        while (bingoData.Count < bingoCount)
        {
            BingoBoardSaveData bingoBoardSaveData = new BingoBoardSaveData(null, null);
            bingoData.Add(bingoBoardSaveData);
        }
        return loadData;
    }

    // GetSlot
    public void SaveBingoSlotData(SerializedDictionary<BingoColumnSlot, List<BingoSlot>> slotList, int bingoIndex = 0)
    {
        List<BingoSlotSaveData> slotSaveDataList = bingoData[bingoIndex].bingoSlotSaveDatas;
        foreach (List<BingoSlot> slot in slotList.Values)
        {
            foreach (BingoSlot s in slot)
            {
                int index = slotSaveDataList.FindIndex(x => x.row == s.Row && x.col == s.Col);
                if (index == -1)
                {
                    //없으면 새로 만들어라
                    slotSaveDataList.Add(new BingoSlotSaveData(s.Row, s.Col, s.Countnum, (int)s.bingoGrade, s.isUnlock));
                }
                else
                {
                    //있으면 값을 바꿔라
                    BingoSlotSaveData bingoSlotSaveData = slotSaveDataList[index];
                    bingoSlotSaveData.count = s.Countnum;
                    bingoSlotSaveData.rarityEnum = (int)s.bingoGrade;
                    bingoSlotSaveData.unlockStates = s.isUnlock;

                    slotSaveDataList[index] = bingoSlotSaveData;
                }
            }
        }
        BingoBoardSaveData boardSaveData = bingoData[bingoIndex];
        boardSaveData.bingoSlotSaveDatas = slotSaveDataList;
        bingoData[bingoIndex] = boardSaveData;

        isDirty = true;
    }

    public void SaveBingoSynergyData(List<BingoSynergy> Synergies, int bingoIndex = 0)
    {
        List<SynergySlotSaveData> synergySaveDataList = bingoData[bingoIndex].synergySlotSaveDatas;
        foreach (BingoSynergy synergy in Synergies)
        {
            int index = synergySaveDataList.FindIndex(x => x.index == synergy.index && x.enumCount == (int)synergy.bingoSynergyLine);
            if (index == -1)
            {
                //없으면 새로 만들어라
                synergySaveDataList.Add(new SynergySlotSaveData(synergy.index, (int)synergy.bingoSynergyLine, synergy.SynergyData.ID));
            }
            else
            {
                SynergySlotSaveData synergyData = synergySaveDataList[index];
                synergyData.synergyID = synergy.SynergyData.ID;
                synergySaveDataList[index] = synergyData;
            }
        }
        BingoBoardSaveData boardSaveData = bingoData[bingoIndex];
        boardSaveData.synergySlotSaveDatas = synergySaveDataList;
        bingoData[bingoIndex] = boardSaveData;

        isDirty = true;
    }

    public void LoadBingoSlotData(BingoContext _ctx, int bingoIndex = 0)
    {
        List<BingoSlotSaveData> slotSaveDataList = bingoData[bingoIndex].bingoSlotSaveDatas;
        foreach (BingoSlotSaveData slotSaveData in slotSaveDataList)
        {
            if (slotSaveData.unlockStates)
                _ctx.Columns[slotSaveData.col].bingoSlotDatas[slotSaveData.row].Countnum = slotSaveData.count;
            _ctx.Columns[slotSaveData.col].bingoSlotDatas[slotSaveData.row].bingoGrade = (RarityType)slotSaveData.rarityEnum;
            _ctx.Columns[slotSaveData.col].bingoSlotDatas[slotSaveData.row].isUnlock = slotSaveData.unlockStates;
        }
    }
    public List<SynergySlotSaveData> LoadSynergyData(int bingoIndex = 0)
    {
        return bingoData[bingoIndex].synergySlotSaveDatas;
    }

    public int FindSynergyID(SynergyDirection enumCountKey, int indexKey, int bingoIndex=0)
    {
        int index;

        List<SynergySlotSaveData> synergyData= bingoData[bingoIndex].synergySlotSaveDatas;

        index = synergyData.FindIndex(x=>x.enumCount==(int)enumCountKey && x.index==indexKey);

        if (index != -1)
        {
            return synergyData[index].synergyID;
        }
        else
        {
            return -1;
        }

    }

    public void ClearDirty()
    {
        isDirty = false;
    }
}
