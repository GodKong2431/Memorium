using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{

    //????? ?????? ??????
    [SerializeField] Transform[] spawnPos;

    //????? ???? ??????, ??? ?????? ?????? ????????? ID ??? ?? EnemyListManager???? ?????´?
    [SerializeField] List<GameObject> enemyPrefab;
    ////????? ???? ???? ?????? <- ??? ???
    [SerializeField] GameObject bossPrefab;
    //???? ??(??)?? ??????? ??? ?? ???? ?? enemyPrefab?? bossPrefab?? ??? ??????? ???
    [SerializeField] int curSpawnGroup = 1;
    //???? ?? ???? ????? ????
    [SerializeField] List<MonsterGroupTable> curSpawnGroupMonsterTable;
    //[SerializeField] List<MonsterGroupTable> curSpawnGroupBossMonsterTable;
    [SerializeField] MonsterGroupTable curSpawnGroupBossMonsterTable;//???? ????? ?? ???? ??????

    [SerializeField] float randomRange=4f;

    [SerializeField] Transform[] maps;
    private int curSpawnerPos = 0;
    Vector3 originPos;

    //??? ??? ?????? ??????? ??????? ????????? ????? ??????δ°? ???? ???????
    IEnumerator Start()
    {
        ChangeParent(maps[curSpawnerPos]);
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        yield return new WaitUntil(() => EnemyListManager.Instance.DataLoad);

        SetMonster();
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
                    Instantiate(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);
                    
                    //GameObject testWizardEnemy = EnemyListManager.Instance.enemyMap[2010012];
                    //Instantiate(testWizardEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);
                }

            }
        }
        else
        {
            //보스 스테이지 진입
            StageManager.Instance.onBossStage = true;
            GameObject spawnBoss = EnemyListManager.Instance.enemyMap[curSpawnGroupBossMonsterTable.MonsterID];
            Instantiate(spawnBoss, spawnPos[spawnPos.Length-1].position, spawnPos[spawnPos.Length - 1].rotation);
            EnemyKillRewardDispatcher.ResetKillCount();
            StageManager.Instance.isReadyToBossSpawn = false;
            //for (int i = 0; i < spawnPos.Length; i++)
            //{
            //    GameObject spawnEnemy = EnemyListManager.Instance.enemyMap[curS];
            //    for (int j = 0; j < curSpawnGroupMonsterTable[i].monsterSpawnCount; j++)
            //    {
            //        Vector3 randX = Random.Range(randomRange, randomRange) * Vector3.right;
            //        Vector3 randZ = Random.Range(randomRange, randomRange) * Vector3.forward;
            //        Instantiate(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);
            //    }
            //}
        }

        curSpawnerPos++;
        if(curSpawnerPos >=maps.Length)
            curSpawnerPos = 0;
        ChangeParent(maps[curSpawnerPos]);
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

        curSpawnerPos = 0;
        ChangeParent(maps[curSpawnerPos]);

        if (StageManager.Instance != null) 
        { 
            int prevSpawnGroup = curSpawnGroup;
            curSpawnGroup = DataManager.Instance.StageManageDict[StageManager.Instance.stageKeyList[StageManager.Instance.curStage - 1]].monsterSpawnGroup;
            if (curSpawnGroup == prevSpawnGroup)
                return;
        }
        curSpawnGroupMonsterTable.Clear();
        //curSpawnGroupBossMonsterTable.Clear();
        enemyPrefab.Clear();

        int i = 0;
        foreach (var monster in DataManager.Instance.MonsterGroupDict)
        {
            //???? ????????? ???? ????? ????? ????? ???? ??????
            if (monster.Value.monsterSpawnGroup == curSpawnGroup)
            {
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
}
