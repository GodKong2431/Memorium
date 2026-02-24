using System;
using System.Collections.Generic;

[System.Serializable]
public class LineQuestTable : TableBase
{
    public string desc;
    public int questNum;
    public QuestType questType;
    public int reqCount;
    public string questTitle;
    public int rewardGroupID;
}
