using UnityEngine;

[System.Serializable]
public class SaveStageData
{
    //현재 진행중인 스테이지, 최대 클리어 스테이지, 현재 스테이지가 실패 상태인지 저장하는 cs
    public int curStage = 0;
    public int maxStage = 0;
    public bool onFailedStage;

    public SaveStageData()
    { }

    public (int cur, int max, bool onFailed) InitStageData()
    {
        return (GetCurStage(), GetMaxStage(), GetOnFailedStage());
    }

    public int GetCurStage()
    {
        if (curStage <= 0)
            curStage = 1;
        return curStage; 
    }
    public int GetMaxStage()
    {
        if(maxStage <= 0)
            maxStage = 1;
        return maxStage;
    }
    public bool GetOnFailedStage()
    {
        return onFailedStage;
    }

    public void Save(int cur, int max, bool onFailed)
    {
        curStage = cur;
        maxStage = max;
        onFailedStage = onFailed;
    }

}
