using System;
using System.Collections.Generic;

[System.Serializable]
public class StageManageTable : TableBase
{
    public string desc;
    public StageType stageType;
    public int stageLevel;
    public int floorNumber;
    public int sceneNumber;
    public string stageName;
    public int monsterKillCount;
    public int dropTableID;
    public int commonMonsterExp;
    public int bossMonsterExp;
    public int monsterSpawnGroup;
    public string mapObject;
}
