using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveStageData :ISaveData
{
    //현재 진행중인 스테이지, 최대 클리어 스테이지, 현재 스테이지가 실패 상태인지 저장하는 cs
    public int curStage = 0;
    //public int maxStage = 0;
    public List<int> maxStage;
    public bool onFailedStage;

    private bool isDirty=false;
    public bool IsDirty => isDirty;

    public SaveStageData()
    { }

    public (int cur, List<int> max, bool onFailed) InitStageData()
    {
        return (GetCurStage(), GetAllMaxStage(), GetOnFailedStage());
    }

    public int GetCurStage()
    {
        if (curStage <= 0)
            curStage = 1;
        return curStage; 
    }
    public List<int> GetAllMaxStage()
    {
        //None부터 시작해서 1 빼야 한다
        int count = Enum.GetValues(typeof(StageType)).Length - 1;
        if (maxStage == null)
        {
            maxStage = new List<int>();
            for (int i = 0; i < count; i++)
                maxStage.Add(0);
        }
        else if (maxStage.Count < count)
        {
            for (int i = maxStage.Count; i < count; i++)
                maxStage.Add(0);
        }
        return maxStage;
    }
    public int GetMaxStage(StageType type)
    {
        if (!maxStage.Contains((int)type - (int)StageType.NormalStage))
            return 0;
        return maxStage[(int)type - (int)StageType.NormalStage];
    }

    public void SetMaxStage(StageType type, int stage)
    {
        if (GetMaxStage(type) < stage)
            maxStage[(int)type - (int)StageType.NormalStage] = stage;
        else
            return;
    }
    public bool GetOnFailedStage()
    {
        return onFailedStage;
    }

    public void Save(int cur, List<int> max, bool onFailed)
    {
        curStage = cur;
        maxStage = max;
        onFailedStage = onFailed;

        isDirty = true;
    }

    public void ClearDirty()
    {
        isDirty = false;
    }

}
