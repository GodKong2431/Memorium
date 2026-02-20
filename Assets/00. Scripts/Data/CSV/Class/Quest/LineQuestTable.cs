using System;
using System.Collections.Generic;

[System.Serializable]
public class LineQuestTable : TableBase
{
    public string questTitle;
    public string questText;
    public QuestType questType;
    public int reqCount;
    public int rewardGroupID;
}
