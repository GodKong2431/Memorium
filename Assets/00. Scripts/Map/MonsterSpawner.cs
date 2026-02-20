using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class MonsterSpawner : MonoBehaviour
{

    //몬스터를 스폰할 스포너
    [SerializeField] Transform[] spawnPos;

    //소환할 몬스터 프리팹, 해당 프리팹 정보는 테이블에서 ID 확인 후 EnemyListManager에서 가져온다
    [SerializeField] List<GameObject> enemyPrefab;
    //소환할 보스 몬스터 프리팹
    [SerializeField] GameObject bossPrefab;
    //현재 층(맵)을 나타내며 해당 값 변경 시 enemyPrefab과 bossPrefab을 모두 변경해야 한다
    [SerializeField] int curSpawnGroup = 1;
    //현재 층 몬스터 테이블 정보
    [SerializeField] List<MonsterGroupTable> curSpawnGroupMonsterTable;
    [SerializeField] List<MonsterGroupTable> curSpawnGroupBossMonsterTable;

    [SerializeField] float randomRange=4f;

    [SerializeField] Transform[] maps;
    private int curSpawnerPos = 0;

    //아래 해당 값들은 매니저는 아니지만 전역객체로 하나씩 저장해두는게 낫지 않을까?
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

        //보스소환조건 만족하면 보스 소환 아니면 잡몹 소환
        if (true)
        {
            for (int i = 0; i < curSpawnGroupMonsterTable.Count; i++)
            {
                GameObject spawnEnemy = EnemyListManager.Instance.enemyMap[curSpawnGroupMonsterTable[i].MonsterID];
                for (int j = 0; j < curSpawnGroupMonsterTable[i].monsterSpawnCount; j++)
                {
                    Vector3 randX = Random.Range(-randomRange, randomRange) * Vector3.right;
                    Vector3 randZ = Random.Range(-randomRange, randomRange) * Vector3.forward;
                    Instantiate(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);
                }
            }
        }
        else
        {
            for (int i = 0; i < spawnPos.Length; i++)
            {
                GameObject spawnEnemy = EnemyListManager.Instance.enemyMap[curSpawnGroupMonsterTable[i].MonsterID];
                for (int j = 0; j < curSpawnGroupMonsterTable[i].monsterSpawnCount; j++)
                {
                    Vector3 randX = Random.Range(randomRange, randomRange) * Vector3.right;
                    Vector3 randZ = Random.Range(randomRange, randomRange) * Vector3.forward;
                    Instantiate(spawnEnemy, spawnPos[i].position + randX + randZ, spawnPos[i].rotation);
                }
            }
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

    //curSpawnGroup 값에 따라서 소환되는 몬스터 설정 -> 층이 바뀌면 cur SpawnGroup 바꾸고 해당 메서드 호출해야 함
    //스테이지 바뀌면 stage +1 하고 해당 메서드 호출해야 함
    private void SetMonster()
    {
        curSpawnGroupMonsterTable.Clear();
        curSpawnGroupBossMonsterTable.Clear();
        enemyPrefab.Clear();

        int i = 0;
        foreach (var monster in DataManager.Instance.MonsterGroupDict)
        {
            //몬스터 테이블에서 현재 맵에서 사용한 몬스터들 정보 가져옴
            if (monster.Value.monsterSpawnGroup == curSpawnGroup)
            {
                //보스몬스터냐 일반 본스터냐에 따라 각각 저장
                if (monster.Value.monsterType == MonsterType.normalMonster)
                {
                    //StageManager.Instance.SetReward(EnemyListManager.Instance.enemyRewardMap[monster.Value.MonsterID], false);
                    curSpawnGroupMonsterTable.Add(monster.Value);
                }
                else
                {
                    curSpawnGroupBossMonsterTable.Add(monster.Value);
                }
                //해당 값은 직렬화로 몬스터 종류 정상적으로 가져왔는지 확인 하기 위한 테스트용 코드
                enemyPrefab.Add(EnemyListManager.Instance.enemyMap[monster.Value.MonsterID]);

                //스포너 이름도 변경한다
                if (i < spawnPos.Length)
                {
                    spawnPos[i].name = monster.Value.spawnerName;
                    i++;
                }
            }
        }
    }
}
