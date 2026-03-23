[System.Serializable]
public class SaveQuestData : ISaveData
{
    //현재 진행 중인 퀘스트 및 진행도를 저장하는 cs
    public int currentQuestId = -1;
    public int currentProgress = 0;

    //변경 여부 체크
    private bool isDirty = false;
    public bool IsDirty => isDirty;

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
    { 
        currentQuestId = id;
        isDirty = true;
    }

    public void SaveProgress(int progress)
    { 
        currentProgress = progress;
        isDirty = true;
    }

    public void Save(int id, int progress)
    {
        currentQuestId = id;
        currentProgress = progress;
        isDirty = true;
    }

    public void ClearDirty()
    {
        isDirty = false;
    }
}
