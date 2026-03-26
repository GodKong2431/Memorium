using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class MonsterSpawner : MonoBehaviour
{
    // 현재 스폰 그룹에서 사용할 몬스터 프리팹 목록
    [Header("Enemy Prefabs")]
    [SerializeField] private List<GameObject> enemyPrefab;
    // 현재 스폰 그룹의 일반 몬스터 행 목록
    [SerializeField] private List<MonsterGroupTable> curSpawnGroupMonsterTable;
    // 현재 스폰 그룹의 보스 몬스터 행
    [SerializeField] private MonsterGroupTable curSpawnGroupBossMonsterTable;

    // 스폰 포인트 이름은 테이블의 spawnerName과 맞춰서 갱신된다
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPos;
    // 현재 적용 중인 몬스터 스폰 그룹 번호
    [SerializeField] private int curSpawnGroup = 1;
    [SerializeField] private float randomRange = 4f;

    // 현재 사용 중인 맵 조각 정보
    [Header("Map")]
    [SerializeField] private List<GameObject> maps;

    private readonly List<GameObject> enemyGroup = new List<GameObject>();

    private int curSpawnerPos;
    private bool isSpawnMonster;

    // 맵과 스테이지 매니저가 준비된 뒤 스폰 기준 데이터를 잡는다
    private IEnumerator Start()
    {
        yield return new WaitUntil(() => MapManager.Instance != null);
        yield return new WaitUntil(() => MapManager.Instance.mapSetting);

        maps = MapManager.Instance.maps;

        yield return new WaitUntil(() => StageManager.Instance != null);
    }

    // 플레이어가 스폰 트리거에 닿으면 현재 그룹 몬스터를 소환한다
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isSpawnMonster)
            return;

        MonsterSpawn();
    }

    // 보스 준비 상태에 따라 일반 몬스터 또는 보스를 소환한다
    public void MonsterSpawn()
    {
        if (isSpawnMonster || StageManager.Instance == null)
            return;

        if (!StageManager.Instance.isReadyToBossSpawn)
        {
            SpawnNormalMonsters();
            return;
        }

        SpawnBossMonster();
    }

    // 현재 맵 조각 기준 스폰 트리거 위치를 다시 맞춘다
    public void ChangeMonsterSpawnPos(Transform parent)
    {
        if (!TryGetCurrentMapInfo(out MapPosInfo mapInfo))
            return;

        transform.position = mapInfo.monsterSpawnTriggerPos.position;
        isSpawnMonster = false;
    }

    // 다음 맵 조각의 스폰 트리거 위치로 이동한다
    public void ChangeMonsterSpawnPos()
    {
        if (maps == null || maps.Count == 0)
            return;

        curSpawnerPos++;
        if (curSpawnerPos >= maps.Count)
            curSpawnerPos = 0;

        if (!TryGetCurrentMapInfo(out MapPosInfo mapInfo))
            return;

        transform.position = mapInfo.monsterSpawnTriggerPos.position;
        isSpawnMonster = false;
    }

    // 처음 맵 조각의 스폰 트리거 위치로 되돌린다
    public void ResetMonsterSpawnPos()
    {
        curSpawnerPos = 0;

        if (!TryGetCurrentMapInfo(out MapPosInfo mapInfo))
            return;

        transform.position = mapInfo.monsterSpawnTriggerPos.position;
        isSpawnMonster = false;
    }

    // 현재 스테이지의 monsterSpawnGroup에 맞춰 스폰 데이터를 갱신한다
    public void SetMonster()
    {
        ClearMonster();

        if (MapManager.Instance == null || DataManager.Instance?.MonsterGroupDict == null)
            return;

        if (maps == null || maps.Count == 0)
            maps = MapManager.Instance.maps;

        curSpawnerPos = 0;

        if (!TryGetCurrentSpawnGroup(out int nextSpawnGroup))
            return;

        bool shouldRebuildGroup =
            curSpawnGroup != nextSpawnGroup ||
            curSpawnGroupMonsterTable == null ||
            curSpawnGroupMonsterTable.Count == 0 ||
            curSpawnGroupBossMonsterTable == null ||
            curSpawnGroupBossMonsterTable.MonsterID <= 0;

        curSpawnGroup = nextSpawnGroup;

        if (shouldRebuildGroup)
            RebuildMonsterGroup();

        MonsterSpawnerReset();
    }

    // 스폰 위치를 초기화하고 현재 그룹 몬스터를 다시 소환한다
    public void MonsterSpawnerReset()
    {
        ResetMonsterSpawnPos();
        isSpawnMonster = false;
        MonsterSpawn();
        ChangeMonsterSpawnPos();
        Debug.Log("[MonsterSpawner] Monster spawner reset.");
    }

    // 맵이 바뀌었을 때 첫 번째 스폰 위치부터 다시 사용한다
    public void MapChange()
    {
        curSpawnerPos = 0;
        ChangeMonsterSpawnPos(maps[curSpawnerPos].transform);
    }

    // 현재 살아 있는 몬스터를 전부 풀로 반환한다
    public void ClearMonster()
    {
        for (int i = enemyGroup.Count - 1; i >= 0; i--)
        {
            GameObject enemy = enemyGroup[i];
            if (enemy == null)
            {
                enemyGroup.RemoveAt(i);
                continue;
            }

            if (enemy.activeSelf)
                ObjectPoolManager.Return(enemy);
        }

        enemyGroup.Clear();
        Debug.Log("[MonsterSpawner] Cleared spawned monsters.");
    }

    private void OnDisable()
    {
        ClearMonster();
    }

    // 현재 그룹의 일반 몬스터들을 순서대로 소환한다
    private void SpawnNormalMonsters()
    {
        if (curSpawnGroupMonsterTable == null || curSpawnGroupMonsterTable.Count == 0)
        {
            Debug.LogWarning($"[MonsterSpawner] No normal monsters configured for spawn group {curSpawnGroup}.");
            return;
        }

        bool spawnedAny = false;
        for (int i = 0; i < curSpawnGroupMonsterTable.Count; i++)
        {
            MonsterGroupTable monsterGroup = curSpawnGroupMonsterTable[i];
            if (monsterGroup == null)
                continue;

            if (!TryGetEnemyPrefab(monsterGroup.MonsterID, out GameObject spawnEnemy))
                continue;

            if (!TryGetSpawnPoint(i, out Transform spawnPoint))
                continue;

            for (int j = 0; j < monsterGroup.monsterSpawnCount; j++)
            {
                Vector3 randX = Random.Range(-randomRange, randomRange) * Vector3.right;
                Vector3 randZ = Random.Range(-randomRange, randomRange) * Vector3.forward;
                GameObject enemy = ObjectPoolManager.Get(spawnEnemy, spawnPoint.position + randX + randZ, spawnPoint.rotation);
                enemy.GetComponent<NavMeshAgent>().Warp(enemy.transform.position);

                if (!enemyGroup.Contains(enemy))
                    enemyGroup.Add(enemy);

                spawnedAny = true;
            }
        }

        isSpawnMonster = spawnedAny;
    }

    // 현재 그룹의 보스 몬스터를 마지막 스폰 포인트에 소환한다
    private void SpawnBossMonster()
    {
        if (curSpawnGroupBossMonsterTable == null || curSpawnGroupBossMonsterTable.MonsterID <= 0)
        {
            Debug.LogWarning($"[MonsterSpawner] No boss monster configured for spawn group {curSpawnGroup}.");
            return;
        }

        if (!TryGetEnemyPrefab(curSpawnGroupBossMonsterTable.MonsterID, out GameObject spawnBoss))
            return;

        if (!TryGetBossSpawnPoint(out Transform bossSpawnPoint))
            return;

        GameObject boss = ObjectPoolManager.Get(spawnBoss, bossSpawnPoint.position, bossSpawnPoint.rotation);
        boss.GetComponent<NavMeshAgent>().Warp(boss.transform.position);
        isSpawnMonster = true;

        if (!enemyGroup.Contains(boss))
            enemyGroup.Add(boss);

        InstanceMessageManager.TryShowBossEnter();
        StageManager.Instance.StartBoss(boss);

        Debug.Log("[MonsterSpawner] Spawned boss monster.");
    }

    // MonsterGroupTable에서 현재 스폰 그룹에 해당하는 행만 다시 모은다
    private void RebuildMonsterGroup()
    {
        curSpawnGroupMonsterTable.Clear();
        enemyPrefab.Clear();
        curSpawnGroupBossMonsterTable = new MonsterGroupTable();

        List<MonsterGroupTable> matchedMonsters = new List<MonsterGroupTable>();
        foreach (MonsterGroupTable monsterGroup in DataManager.Instance.MonsterGroupDict.Values)
        {
            if (monsterGroup == null || monsterGroup.monsterSpawnGroup != curSpawnGroup)
                continue;

            matchedMonsters.Add(monsterGroup);
        }

        matchedMonsters.Sort(CompareSpawnEntries);

        int spawnPointIndex = 0;
        for (int i = 0; i < matchedMonsters.Count; i++)
        {
            MonsterGroupTable monsterGroup = matchedMonsters[i];
            if (monsterGroup.monsterType == MonsterType.normalMonster)
            {
                curSpawnGroupMonsterTable.Add(monsterGroup);

                if (spawnPointIndex < spawnPos.Length && spawnPos[spawnPointIndex] != null)
                {
                    spawnPos[spawnPointIndex].name = monsterGroup.spawnerName;
                    spawnPointIndex++;
                }
            }
            else
            {
                curSpawnGroupBossMonsterTable = monsterGroup;
            }

            if (TryGetEnemyPrefab(monsterGroup.MonsterID, out GameObject prefab))
                enemyPrefab.Add(prefab);
        }

        if (curSpawnGroupMonsterTable.Count == 0)
        {
            Debug.LogWarning(
                $"[MonsterSpawner] Spawn group {curSpawnGroup} has no normal monster rows. " +
                "Check StageManageTable.monsterSpawnGroup and MonsterGroupTable ids.");
        }
    }

    // 현재 스테이지가 요구하는 monsterSpawnGroup 값을 읽어온다
    private bool TryGetCurrentSpawnGroup(out int spawnGroup)
    {
        spawnGroup = 0;

        if (StageManager.Instance == null || StageManager.Instance.stageKeyList == null)
            return false;

        int stageIndex = StageManager.Instance.curStage - 1;
        if (stageIndex < 0 || stageIndex >= StageManager.Instance.stageKeyList.Count)
            return false;

        int stageKey = StageManager.Instance.stageKeyList[stageIndex];
        if (!DataManager.Instance.StageManageDict.TryGetValue(stageKey, out StageManageTable stageData) || stageData == null)
            return false;

        spawnGroup = stageData.monsterSpawnGroup;
        return true;
    }

    // 현재 맵 조각의 위치 정보 컴포넌트를 안전하게 가져온다
    private bool TryGetCurrentMapInfo(out MapPosInfo mapInfo)
    {
        mapInfo = null;

        if (MapManager.Instance == null ||
            MapManager.Instance.mapPosInfo == null ||
            curSpawnerPos < 0 ||
            curSpawnerPos >= MapManager.Instance.mapPosInfo.Count)
        {
            return false;
        }

        mapInfo = MapManager.Instance.mapPosInfo[curSpawnerPos];
        return mapInfo != null && mapInfo.monsterSpawnTriggerPos != null;
    }

    // 일반 몬스터 스폰에 사용할 포인트를 인덱스로 가져온다
    private bool TryGetSpawnPoint(int index, out Transform spawnPoint)
    {
        spawnPoint = null;

        if (!TryGetCurrentMapInfo(out MapPosInfo mapInfo))
            return false;

        if (mapInfo.monsterSpawnPos == null || index < 0 || index >= mapInfo.monsterSpawnPos.Length)
        {
            Debug.LogWarning($"[MonsterSpawner] Missing spawn point {index} for map index {curSpawnerPos}.");
            return false;
        }

        spawnPoint = mapInfo.monsterSpawnPos[index];
        return spawnPoint != null;
    }

    // 보스 스폰에 사용할 마지막 포인트를 가져온다
    private bool TryGetBossSpawnPoint(out Transform spawnPoint)
    {
        spawnPoint = null;

        if (!TryGetCurrentMapInfo(out MapPosInfo mapInfo))
            return false;

        if (mapInfo.monsterSpawnPos == null || mapInfo.monsterSpawnPos.Length == 0)
        {
            Debug.LogWarning($"[MonsterSpawner] Missing boss spawn point for map index {curSpawnerPos}.");
            return false;
        }

        spawnPoint = mapInfo.monsterSpawnPos[mapInfo.monsterSpawnPos.Length - 1];
        return spawnPoint != null;
    }

    // 몬스터 ID로 실제 적 프리팹을 찾는다
    private bool TryGetEnemyPrefab(int monsterId, out GameObject prefab)
    {
        prefab = null;

        if (EnemyListManager.Instance == null || EnemyListManager.Instance.enemyMap == null)
            return false;

        if (!EnemyListManager.Instance.enemyMap.TryGetValue(monsterId, out prefab) || prefab == null)
        {
            Debug.LogWarning($"[MonsterSpawner] Missing enemy prefab for monster id {monsterId}.");
            return false;
        }

        return true;
    }

    // 스폰 순서를 일정하게 유지하기 위해 테이블 항목을 정렬한다
    private static int CompareSpawnEntries(MonsterGroupTable left, MonsterGroupTable right)
    {
        if (ReferenceEquals(left, right))
            return 0;
        if (left == null)
            return 1;
        if (right == null)
            return -1;

        int typeCompare = left.monsterType.CompareTo(right.monsterType);
        if (typeCompare != 0)
            return typeCompare;

        int spawnerTypeCompare = left.spawnerType.CompareTo(right.spawnerType);
        if (spawnerTypeCompare != 0)
            return spawnerTypeCompare;

        int spawnerNameCompare = string.CompareOrdinal(left.spawnerName, right.spawnerName);
        if (spawnerNameCompare != 0)
            return spawnerNameCompare;

        return left.MonsterID.CompareTo(right.MonsterID);
    }
}
