using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{

    [Header("몬스터 프리팹")]
    // 일반 몬스터 프리팹 목록 (EnemyListManager.enemyMap에서 동적 로드하므로 여기선 미사용. 새 몬스터 프리팹은 EnemyListManager.enemyList에 추가)
    [SerializeField] List<GameObject> enemyPrefab; // 몬스터 프리팹 추가 예정
    //[SerializeField] GameObject bossPrefab;         // 보스 몬스터 프리팹 추가 예정

    // 현재 그룹의 일반 몬스터 테이블
    [SerializeField] List<MonsterGroupTable> curSpawnGroupMonsterTable;
    [SerializeField] MonsterGroupTable curSpawnGroupBossMonsterTable; // 현재 그룹의 보스 몬스터 테이블
    //[SerializeField] List<MonsterGroupTable> curSpawnGroupBossMonsterTable;

    [Header("소환 정보")]
    // 스폰 위치 목록
    [SerializeField] Transform[] spawnPos;

    // 현재 적용 중인 스폰 그룹 번호
    [SerializeField] int curSpawnGroup = 1;

    [SerializeField] float randomRange=4f;

    [Header("소환될 맵")]
    [SerializeField] List<GameObject> maps;
    private int curSpawnerPos = 0;
    //[SerializeField] private GameObject[] mapGroups;
    //Vector3 originPos;

    List<GameObject> enemyGroup=new List<GameObject>();

    // 매니저와 맵 설정이 준비될 때까지 대기한 뒤 맵 참조를 가져온다.
    IEnumerator Start()
    {
        yield return new WaitUntil(() => MapManager.Instance != null);
        yield return new WaitUntil(() => MapManager.Instance.mapSetting);

        //매니저에 있는 맵 참조<- 주소를 참조하여 이후에도 동기화 진행
        maps = MapManager.Instance.maps;

        yield return new WaitUntil(() => StageManager.Instance != null);

        //스테이지 클리어 혹은 패배 시 몬스터 청소
        StageManager.Instance.OnStageClearOrFailed += ClearMonster;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        // 보스 소환 준비 여부에 따라 일반 몬스터 또는 보스를 스폰
        if (!StageManager.Instance.isReadyToBossSpawn)
        {
            for (int i = 0; i < curSpawnGroupMonsterTable.Count; i++)
            {
                GameObject spawnEnemy = EnemyListManager.Instance.enemyMap[curSpawnGroupMonsterTable[i].MonsterID];
                for (int j = 0; j < curSpawnGroupMonsterTable[i].monsterSpawnCount; j++)
                {
                    Vector3 randX = Random.Range(-randomRange, randomRange) * Vector3.right;
                    Vector3 randZ = Random.Range(-randomRange, randomRange) * Vector3.forward;
                    //오브젝트 풀링으로 전환
                    GameObject enemy = ObjectPoolManager.Get(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);

                    if(!enemyGroup.Contains(enemy))
                        enemyGroup.Add(enemy);

                    // 스폰 이펙트 추가 예정
                    // 스폰 효과음 추가 예정
                }

            }
        }
        else
        {
            //보스 스테이지 진입
            GameObject spawnBoss = EnemyListManager.Instance.enemyMap[curSpawnGroupBossMonsterTable.MonsterID];

            ////오브젝트 풀링으로 전환
            GameObject boss = ObjectPoolManager.Get(spawnBoss, spawnPos[spawnPos.Length - 1].position, spawnPos[spawnPos.Length - 1].rotation);

            if(!enemyGroup.Contains(boss))
                enemyGroup.Add(boss);

            // 보스 스폰 이펙트 추가 예정
            // 보스 스폰 효과음 추가 예정
            InstanceMessageManager.TryShowBossEnter();
            StageManager.Instance.StartBoss(boss);
        }

        curSpawnerPos++;
        if(curSpawnerPos >=maps.Count)
            curSpawnerPos = 0;
        ChangeParent(maps[curSpawnerPos].transform);
    }

    private void ChangeParent(Transform parent)
    {
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
    }

    // 현재 스테이지에 맞는 스폰 그룹을 다시 구성하고 스폰 데이터를 갱신
    public void SetMonster()
    {
        if (maps.Count <= 0)
            maps = MapManager.Instance.maps;
        curSpawnerPos = 0;
        //if (maps.Count <= 0)
        //{
        //    SetMap(1);
        //}
        ChangeParent(maps[curSpawnerPos].transform);

        if (StageManager.Instance != null) 
        { 
            int prevSpawnGroup = curSpawnGroup;
            curSpawnGroup = DataManager.Instance.StageManageDict[StageManager.Instance.stageKeyList[StageManager.Instance.curStage - 1]].monsterSpawnGroup;
            if (curSpawnGroup == prevSpawnGroup)
            {

                return;
            }
        }
        curSpawnGroupMonsterTable.Clear();
        enemyPrefab.Clear();


        int i = 0;
        foreach (var monster in DataManager.Instance.MonsterGroupDict)
        {
                // 현재 그룹에 해당하는 몬스터만 수집
                if (monster.Value.monsterSpawnGroup == curSpawnGroup)
                {

                // 일반 몬스터와 보스 몬스터 테이블을 분리
                if (monster.Value.monsterType == MonsterType.normalMonster)
                {
                    //StageManager.Instance.SetReward(EnemyListManager.Instance.enemyRewardMap[monster.Value.MonsterID], false);
                    curSpawnGroupMonsterTable.Add(monster.Value);
                }
                else
                {
                    //curSpawnGroupBossMonsterTable.Add(monster.Value);
                    curSpawnGroupBossMonsterTable=monster.Value;
                }
                // 스폰 가능한 프리팹 목록을 함께 갱신
                enemyPrefab.Add(EnemyListManager.Instance.enemyMap[monster.Value.MonsterID]);

                // 스폰 포인트 이름을 테이블 기준으로 설정
                if (i < spawnPos.Length)
                {
                    spawnPos[i].name = monster.Value.spawnerName;
                    i++;
                }
            }
        }
    }

    public void MapChange()
    {
        curSpawnerPos = 0;
        ChangeParent(maps[curSpawnerPos].transform);
    }

    public void ClearMonster()
    {
        foreach (GameObject enemy in enemyGroup)
        {
            if(enemy.activeSelf)
                ObjectPoolManager.Return(enemy);
        }
    }

    private void OnDisable()
    {
        ClearMonster();
    }

    //curFloor-1 맵을 불러와 맵을 바꾸는 것을 목적으로 함
    //public void SetMap(int curFloor)
    //{
    //    if (maps == null)
    //        maps = new List<GameObject>();
    //    maps.Clear();
    //    for (int i = 0; i < mapGroups[0].transform.childCount; i++)
    //    {
    //        maps.Add(mapGroups[0].transform.GetChild(i).gameObject);
    //    }
    //}

}
