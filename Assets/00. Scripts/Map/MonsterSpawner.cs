using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{

    //????? ?????? ??????
    [SerializeField] Transform[] spawnPos;

    // 일반 몬스터 프리팹 목록 (EnemyListManager.enemyMap에서 동적 로드하므로 여기선 미사용. 새 몬스터 프리팹은 EnemyListManager.enemyList에 추가)
    [SerializeField] List<GameObject> enemyPrefab; // 몬스터 프리팹 추가 예정
    [SerializeField] GameObject bossPrefab;         // 보스 몬스터 프리팹 추가 예정
    //???? ??(??)?? ??????? ??? ?? ???? ?? enemyPrefab?? bossPrefab?? ??? ??????? ???
    [SerializeField] int curSpawnGroup = 1;
    //???? ?? ???? ????? ????
    [SerializeField] List<MonsterGroupTable> curSpawnGroupMonsterTable;
    //[SerializeField] List<MonsterGroupTable> curSpawnGroupBossMonsterTable;
    [SerializeField] MonsterGroupTable curSpawnGroupBossMonsterTable;//???? ????? ?? ???? ??????

    [SerializeField] float randomRange=4f;

    [SerializeField] List<GameObject> maps;
    [SerializeField] private GameObject[] mapGroups;
    private int curSpawnerPos = 0;
    Vector3 originPos;


    //??? ??? ?????? ??????? ??????? ????????? ????? ??????δ°? ???? ???????
    IEnumerator Start()
    {
        yield return new WaitUntil(() => MapManager.Instance != null);
        yield return new WaitUntil(() => MapManager.Instance.mapSetting);

        //매니저에 있는 맵 참조<- 주소를 참조하여 이후에도 동기화 진행
        maps = MapManager.Instance.maps;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        //??????????? ??????? ???? ??? ???? ??? ???
        if (!StageManager.Instance.isReadyToBossSpawn)
        {
            for (int i = 0; i < curSpawnGroupMonsterTable.Count; i++)
            {
                GameObject spawnEnemy = EnemyListManager.Instance.enemyMap[curSpawnGroupMonsterTable[i].MonsterID];
                for (int j = 0; j < curSpawnGroupMonsterTable[i].monsterSpawnCount; j++)
                {
                    Vector3 randX = Random.Range(-randomRange, randomRange) * Vector3.right;
                    Vector3 randZ = Random.Range(-randomRange, randomRange) * Vector3.forward;
                    ////오브젝트 풀링으로 전환
                    //Instantiate(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);
                    ObjectPoolManager.Get(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);

                    // 스폰 이펙트 추가 예정
                    // 스폰 효과음 추가 예정
                }

            }
        }
        else
        {
            //보스 스테이지 진입
            StageManager.Instance.onBossStage = true;
            GameObject spawnBoss = EnemyListManager.Instance.enemyMap[curSpawnGroupBossMonsterTable.MonsterID];

            ////오브젝트 풀링으로 전환
            //Instantiate(spawnBoss, spawnPos[spawnPos.Length-1].position, spawnPos[spawnPos.Length - 1].rotation);
            ObjectPoolManager.Get(spawnBoss, spawnPos[spawnPos.Length - 1].position, spawnPos[spawnPos.Length - 1].rotation);

            // 보스 스폰 이펙트 추가 예정
            // 보스 스폰 효과음 추가 예정
            EnemyKillRewardDispatcher.ResetKillCount();
            StageManager.Instance.isReadyToBossSpawn = false;
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

    //curSpawnGroup ???? ???? ?????? ???? ???? -> ???? ???? cur SpawnGroup ???? ??? ????? ?????? ??
    //???????? ???? stage +1 ??? ??? ????? ?????? ?? <- ???????? ???? ????? ???????
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
                Debug.Log("[MonsterSpawner] 몬스터 추가 실패 및 반환");
                return;
            }
        }
        curSpawnGroupMonsterTable.Clear();
        enemyPrefab.Clear();

        Debug.Log("[MonsterSpawner] 몬스터 추가 직전");
        int i = 0;
        foreach (var monster in DataManager.Instance.MonsterGroupDict)
        {
            //???? ????????? ???? ????? ????? ????? ???? ??????
            if (monster.Value.monsterSpawnGroup == curSpawnGroup)
            {
                Debug.Log("[MonsterSpawner] 몬스터 추가");
                //????????? ??? ??????Ŀ? ???? ???? ????
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
                //??? ???? ??????? ???? ???? ?????????? ????????? ??? ??? ???? ?????? ???
                enemyPrefab.Add(EnemyListManager.Instance.enemyMap[monster.Value.MonsterID]);

                //?????? ????? ???????
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
