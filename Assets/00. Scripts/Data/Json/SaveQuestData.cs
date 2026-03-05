using UnityEngine;

[System.Serializable]
public class SaveQuestData
{
    //현재 진행 중인 퀘스트 및 진행도를 저장하는 cs
    public int currentQuestId = -1;
    public int currentProgress = 0;

    public SaveQuestData()
    { }

    public (int id, int progress) InitQuestData()
    {
        return (ReturnQuestId(), ReturnProgress());
    }
    public int ReturnQuestId()
    {
        if (currentQuestId <= 0)
        {
            return 7010001;
        }
        return currentQuestId;
    }
    public int ReturnProgress()
    {
        return currentProgress;
    }
    public void SaveID(int id)
    { currentQuestId = id; }

    public void SaveProgress(int progress)
    { currentProgress = progress; }

    public void Save(int id, int progress)
    {
        currentQuestId = id;
        currentProgress = progress;
    }
}
