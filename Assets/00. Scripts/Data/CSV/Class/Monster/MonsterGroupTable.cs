using System;
using System.Collections.Generic;

[System.Serializable]
public class MonsterGroupTable : TableBase
{
    public int monsterSpawnGroup;
    public SpawnerType spawnerType;
    public string spawnerName;
    public MonsterType monsterType;
    public int MonsterID;
    public int monsterSpawnCount;
}
