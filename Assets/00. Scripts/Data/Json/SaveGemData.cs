using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveGemData : ISaveData
{
    public List<GemSaveData> gemSaveData;
    private bool isDirty = false;
    public bool IsDirty => isDirty;


    public SaveGemData()
    {
        
    }

    public bool InitGemData()
    {
        if (gemSaveData == null)
        {
            gemSaveData = new List<GemSaveData>();
            return false;
        }
        return true;
    }

    public void SaveGemInfoData(List<GemSaveData> data)
    {
        gemSaveData.Clear();
        foreach (GemSaveData i in data)
        {
            int index = gemSaveData.FindIndex(x=> x.gemId == i.gemId && x.grade == i.grade);
            {
                if (index == -1)
                {
                    gemSaveData.Add(i);
                }
                else
                {
                    gemSaveData[index] = i;
                }
            }
        }
        isDirty = true;
    }

    public List<GemSaveData> LoadGemInfoData()
    {
        return gemSaveData;
    }


    public void ClearDirty()
    {
        isDirty = false;
    }

}
