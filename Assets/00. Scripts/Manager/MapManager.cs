using System.Collections.Generic;
using UnityEngine;

public class MapManager : Singleton<MapManager>
{
    //생성할 맵 프리팹
    [SerializeField] private GameObject[] normalMapGroupsPrefab;
    [SerializeField] private GameObject[] goldDungeonMapGroupPrefab;
    [SerializeField] private GameObject[] expDungeonMapGroupPrefab;
    [SerializeField] private GameObject[] itemFarmingDungeonMapGroupPrefab;
    [SerializeField] private GameObject[] rareItemDungeonMapGroupPrefab;
    GameObject[] mapGroupPrefab;
    int curMapFloor = 0;
    StageType curMapStageType;

    Dictionary<StageType, GameObject[]> mapGroupPrefabByStageType;

    public bool mapSetting=false;
    public List<GameObject> maps = new List<GameObject>();
    public Dictionary<int, GameObject> mapGroups = new Dictionary<int, GameObject>();

    protected override void Awake()
    {
        base.Awake();
        mapGroupPrefabByStageType = new Dictionary<StageType, GameObject[]>();
        mapGroupPrefabByStageType[StageType.NormalStage] = normalMapGroupsPrefab;
        mapGroupPrefabByStageType[StageType.GuardianTaxVault] = goldDungeonMapGroupPrefab;
        mapGroupPrefabByStageType[StageType.HallOfTraining] = expDungeonMapGroupPrefab;
        mapGroupPrefabByStageType[StageType.CelestiAlchemyWorkshop] = itemFarmingDungeonMapGroupPrefab;
        mapGroupPrefabByStageType[StageType.EidosTreasureVault] = rareItemDungeonMapGroupPrefab;

        //현재 하드 코딩으로 1로 시작하나, 이후엔 데이터를 받아와 원하는 층 불러올 예정
        //MapSetting(1);
    }

    //public void MapSetting(int curFloor)
    //{
    //    if (curMapFloor == curFloor)
    //        return;
    //    if (maps == null)
    //        maps = new List<GameObject>();

    //    //층이 올라가거나 내려가는 것 모두 대비 <- 1층의 맵은 ~~다
    //    //어차피 씬 넘어가면 딕셔너리가 참조하던 맵든 전부 null처리 되서 새로 생긴다
    //    if (!mapGroups.ContainsKey(curFloor) || mapGroups[curFloor]==null)
    //        mapGroups[curFloor] = Instantiate(normalMapGroupsPrefab[curFloor - 1]);
    //    maps.Clear();
    //    for (int i = 0; i < mapGroups[curFloor].transform.childCount; i++)
    //    {
    //        maps.Add(mapGroups[curFloor].transform.GetChild(i).gameObject);
    //    }
        
    //    mapSetting=true;
    //}
    public void MapSetting(StageType curStageType, int curFloor)
    {
        //처음 쵝화 시에는 현재 층이 0이라 스테이지 타입 같아도 상관 없음
        if (curMapFloor == curFloor && curMapStageType == curStageType)
            return;

        //이전 층의 맵은 비활성화
        if (mapGroups.ContainsKey(curMapFloor) && mapGroups[curMapFloor]!=null)
            mapGroups[curMapFloor].SetActive(false);

        curMapFloor = curFloor;
        curMapStageType = curStageType;
        mapGroupPrefab = mapGroupPrefabByStageType[curStageType];
        if (maps == null)
            maps = new List<GameObject>();
        //층이 올라가거나 내려가는 것 모두 대비 <- 1층의 맵은 ~~다
        if (!mapGroups.ContainsKey(curFloor) || mapGroups[curFloor] == null)
            mapGroups[curFloor] = Instantiate(mapGroupPrefab[curFloor - 1]);

        mapGroups[curMapFloor].SetActive(true);

        maps.Clear();
        for (int i = 0; i < mapGroups[curFloor].transform.childCount; i++)
        {
            maps.Add(mapGroups[curFloor].transform.GetChild(i).gameObject);
        }
        
        mapSetting=true;
    }
}