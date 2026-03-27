using System.Collections.Generic;
using UnityEngine;

public class MapManager : Singleton<MapManager>
{
    // Map prefabs grouped by stage type.
    [SerializeField] private GameObject[] normalMapGroupsPrefab;
    [SerializeField] private GameObject[] goldDungeonMapGroupPrefab;
    [SerializeField] private GameObject[] expDungeonMapGroupPrefab;
    [SerializeField] private GameObject[] itemFarmingDungeonMapGroupPrefab;
    [SerializeField] private GameObject[] rareItemDungeonMapGroupPrefab;

    [SerializeField] int[] normalMapBGMId;
    [SerializeField] int[] goldDungeonMapBGMId;
    [SerializeField] int[] expDungeonMapBGMId;
    [SerializeField] int[] itemFarmingDungeonMapBGMId;
    [SerializeField] int[] rareItemDungeonMapBGMId;


    GameObject[] mapGroupPrefab;
    int curMapFloor = 0;
    StageType curMapStageType;

    Dictionary<StageType, GameObject[]> mapGroupPrefabByStageType;
    Dictionary<StageType, int[]> bgmIdByStageType;

    public List<MapPosInfo> mapPosInfo = new List<MapPosInfo>();

    public bool mapSetting = false;
    public List<GameObject> maps = new List<GameObject>();
    public Dictionary<int, GameObject> mapGroups = new Dictionary<int, GameObject>();

    public bool readyToMonsterSpawnerMove = false;

    private static int GetMapGroupKey(StageType stageType, int floor)
    {
        return ((int)stageType * 1000) + Mathf.Max(0, floor);
    }

    private bool TryGetLiveMapGroup(StageType stageType, int floor, out GameObject mapGroup)
    {
        int key = GetMapGroupKey(stageType, floor);
        if (mapGroups.TryGetValue(key, out mapGroup) && mapGroup != null)
            return true;

        mapGroups.Remove(key);
        mapGroup = null;
        return false;
    }

    protected override void Awake()
    {
        base.Awake();
        mapGroupPrefabByStageType = new Dictionary<StageType, GameObject[]>();
        mapGroupPrefabByStageType[StageType.NormalStage] = normalMapGroupsPrefab;
        mapGroupPrefabByStageType[StageType.GuardianTaxVault] = goldDungeonMapGroupPrefab;
        mapGroupPrefabByStageType[StageType.HallOfTraining] = expDungeonMapGroupPrefab;
        mapGroupPrefabByStageType[StageType.CelestiAlchemyWorkshop] = itemFarmingDungeonMapGroupPrefab;
        mapGroupPrefabByStageType[StageType.EidosTreasureVault] = rareItemDungeonMapGroupPrefab;

        bgmIdByStageType = new Dictionary<StageType, int[]>();
        bgmIdByStageType[StageType.NormalStage] = normalMapBGMId;
        bgmIdByStageType[StageType.GuardianTaxVault] = goldDungeonMapBGMId;
        bgmIdByStageType[StageType.HallOfTraining] = expDungeonMapBGMId;
        bgmIdByStageType[StageType.CelestiAlchemyWorkshop] = itemFarmingDungeonMapBGMId;
        bgmIdByStageType[StageType.EidosTreasureVault] = rareItemDungeonMapBGMId;
    }

    public void MapSetting(StageType curStageType, int curFloor)
    {
        bool hasCurrentLiveMap =
            curMapFloor > 0 &&
            TryGetLiveMapGroup(curMapStageType, curMapFloor, out _);

        // This singleton survives scene loads, but the spawned map objects do not.
        // Only skip setup when the cached map object is still alive.
        if (curMapFloor == curFloor &&
            curMapStageType == curStageType &&
            hasCurrentLiveMap)
        {
            return;
        }

        if (TryGetLiveMapGroup(curMapStageType, curMapFloor, out GameObject previousMapGroup))
            previousMapGroup.SetActive(false);

        curMapFloor = curFloor;
        curMapStageType = curStageType;
        mapGroupPrefab = mapGroupPrefabByStageType[curStageType];
        if (maps == null)
            maps = new List<GameObject>();

        int targetKey = GetMapGroupKey(curStageType, curFloor);
        if (!TryGetLiveMapGroup(curStageType, curFloor, out GameObject currentMapGroup))
        {
            currentMapGroup = Instantiate(mapGroupPrefab[curFloor - 1]);
            mapGroups[targetKey] = currentMapGroup;
        }

        ////bgmKey 가져오기
        int bgmKey = bgmIdByStageType[curStageType][curFloor - 1];
        SoundManager.Instance.PlayBgm(bgmKey);

        currentMapGroup.SetActive(true);

        maps.Clear();
        mapPosInfo.Clear();
        for (int i = 0; i < currentMapGroup.transform.childCount; i++)
        {
            maps.Add(currentMapGroup.transform.GetChild(i).gameObject);
            mapPosInfo.Add(maps[i].GetComponent<MapPosInfo>());
        }

        mapSetting = true;
    }
}
