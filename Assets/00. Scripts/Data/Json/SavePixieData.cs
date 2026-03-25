using System;
using System.Collections.Generic;

[Serializable]
public class SavePixieData:ISaveData
{
    public List<PixieSaveData> pixieSaveData;

    public int equippedPixieId;

    private bool isDirty = false;
    public bool IsDirty => isDirty;

    public SavePixieData() { }

    public bool InitPixieData()
    {

        if (pixieSaveData == null)
        {
            pixieSaveData = new List<PixieSaveData>();
            return false;
        }
        return true;
    }

    public void SavePixieInfoData(List<PixieSaveData> data)
    {
        pixieSaveData.Clear();
        foreach (PixieSaveData i in data)
        {
            int index = pixieSaveData.FindIndex
                (x => x.pixieId == i.pixieId);
            if (index == -1)
            {
                pixieSaveData.Add(i);
            }
            else
            {
                pixieSaveData[index] = i;
            }
        } 
        isDirty = true;
    }

    public void SavePixieInfoDataByDic(Dictionary<int, OwnedPixieData> pixieDataDic)
    {
        foreach (var pixieData in pixieDataDic)
        {

            int index = pixieSaveData.FindIndex
                (x => x.pixieId == pixieData.Key);
            if (index == -1)
            {
                PixieSaveData data = new PixieSaveData(pixieData.Key, pixieData.Value.level);
                pixieSaveData.Add(data);
            }
            else
            {
                PixieSaveData data = pixieSaveData[index];
                data.level = pixieData.Value.level;
                pixieSaveData[index] =data;
            }
        }
        isDirty = true;
    }

    public List<PixieSaveData> LoadPixieInfoData()
    {
        return pixieSaveData;
    }

    public void SavePixieEquippedId(int id)
    { 
        equippedPixieId = id;
        isDirty = true;
    }

    public void ClearDirty()
    {
        isDirty = false;
    }
}
