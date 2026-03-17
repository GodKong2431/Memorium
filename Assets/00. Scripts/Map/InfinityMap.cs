using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InfinityMap : MonoBehaviour

{ 
    //플레이어와 충돌 시 맵을 이동시키는 트리거
    [SerializeField] private GameObject mapMoveTrigger;

    //현재 트리거가 설치된 맵 인덱스
    [SerializeField] int curMapIndex = 1;
    [SerializeField] Vector3 originPlayerPos;

    //목표 위치
    [SerializeField] Transform goalTransform;
    Goal goal;

    //플레이어
    [SerializeField] Transform player;
    [SerializeField] Transform pixie;

    [SerializeField] private GameObject[] mapGroupsPrefab;

    private readonly List<Renderer> mapsRenderer = new List<Renderer>();
    private readonly List<Vector3> originMapPos = new List<Vector3>();

    private Vector3 originTriggerPos;
    private NavMeshAgent agent;
    private PixieSpawner pixieSpawner;

    public List<GameObject> mapGroups;
    public List<GameObject> maps;
    public bool firstMapSetting = false;

    [SerializeField] MonsterSpawner monsterSpawner;

    private void OnEnable()
    {
        GameEventManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerSpawned -= OnPlayerSpawned;
    }


    IEnumerator Start()
    {
        yield return new WaitUntil(() => MapManager.Instance != null);
        yield return new WaitUntil(() => MapManager.Instance.mapSetting);
        

        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance.infinityMap = this;


        //매니저에 있는 맵 참조<- 주소를 참조하여 이후에도 동기화 진행
        maps = MapManager.Instance.maps;
        MapInit();

        goal = goalTransform.GetComponent<Goal>();
        goal.goalTriggerOn += SetGoalPos;
        goal.ResetTrigger();

    }

    public void MapInit()
    {
        mapsRenderer.Clear();
        originMapPos.Clear();
        curMapIndex = 1;

        for (int i = 0; i < maps.Count; i++)
        {
            mapsRenderer.Add(maps[i].GetComponent<Renderer>());

            if (i > 0)
            {
                float plusPosition = mapsRenderer[i].bounds.extents.x + mapsRenderer[i - 1].bounds.extents.x;
                maps[i].transform.position = maps[i - 1].transform.position + Vector3.right * plusPosition;
            }

            originMapPos.Add(maps[i].transform.position);
        }

        ResetGoalPos();

        mapMoveTrigger.transform.position = MapManager.Instance.mapPosInfo[curMapIndex].mapMoveTriggerPos.position;

        originTriggerPos = mapMoveTrigger.transform.position;

        firstMapSetting = true;

        PlayerPosInit();

    }

    private void OnTriggerEnter(Collider other)
    {
        if (mapsRenderer.Count == 0)
            return;
        if (!other.CompareTag("Player"))
            return;

        MapMove();
    }

    public void MapMove()
    {
        int moveMapIndex = curMapIndex - 1;
        if (moveMapIndex < 0)
            moveMapIndex = maps.Count - 1;

        int furthestMapIndex = moveMapIndex - 1;
        if (furthestMapIndex < 0)
            furthestMapIndex = maps.Count - 1;

        //이동할 맵 이전 맵은 제일 끝에 있는 맵이므로 해당 맵 정보 가져온 다음 거기에 포지션을 추가
        float plusPosition = mapsRenderer[moveMapIndex].bounds.extents.x + mapsRenderer[furthestMapIndex].bounds.extents.x;
        maps[moveMapIndex].transform.position = maps[furthestMapIndex].transform.position + Vector3.right * plusPosition;
        //Debug.Log($"[InfinityMap] 맵 이동 완료");

        //다음 맵으로 트리거 이동
        int nextMapIndex = curMapIndex + 1;
        if (nextMapIndex >= maps.Count)
            nextMapIndex = 0;


        mapMoveTrigger.transform.position = MapManager.Instance.mapPosInfo[nextMapIndex].mapMoveTriggerPos.position;

        curMapIndex = nextMapIndex;

        if (monsterSpawner != null)
            monsterSpawner.ChangeMonsterSpawnPos();
    }

    public void MapReset()
    {
        for (int i = 0; i < maps.Count; i++)
            maps[i].transform.position = originMapPos[i];

        mapMoveTrigger.transform.position = originTriggerPos;
        curMapIndex = 1;

        ResetGoalPos();
        PlayerPosInit();

        //monsterSpawner.MonsterSpawnerReset();
    }
    public void SetGoalPos()
    {
        int goalIndex = curMapIndex;
        if (goalIndex >= maps.Count) goalIndex = 0;

        goalTransform.transform.position = MapManager.Instance.mapPosInfo[goalIndex].goalPos.position;
        
        // 플레이어 에이전트에게 새 목적지 전달 및 경로 초기화
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath(); // 이전의 잘못된 경로 데이터 삭제

            // NavMesh 위의 유효한 좌표인지 한 번 더 검사 (이미지 이슈 해결)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(goalTransform.transform.position, out hit, 10.0f, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
        }
        MapManager.Instance.readyToMonsterSpawnerMove = true;

        goal.ResetTrigger();
    }

    public void ResetGoalPos()
    {
        int goalIndex = 0;
        goalTransform.transform.position = MapManager.Instance.mapPosInfo[goalIndex].goalPos.position;
        MapManager.Instance.readyToMonsterSpawnerMove = true;
    }

    public void PlayerPosInit()
    {
        bool hadPlayerBinding = HasPlayerBinding();
        if (!TryBindScenePlayer())
            return;

        if (!hadPlayerBinding)
        {
            MovePlayer(ResolveNavMeshPosition(player.position));
            originPlayerPos = player.position;
            return;
        }

        MovePlayer(originPlayerPos);
        MovePixieToPlayer();
    }

    private void OnPlayerSpawned(Transform spawnedPlayer)
    {
        if (spawnedPlayer == null)
            return;

        bool hadPlayerBinding = HasPlayerBinding();
        BindPlayer(spawnedPlayer);

        if (firstMapSetting && !hadPlayerBinding)
            PlayerPosInit();
    }

    private bool TryBindScenePlayer()
    {
        if (HasPlayerBinding())
            return true;

        if (!ScenePlayerLocator.TryGetPlayerTransform(out Transform scenePlayer))
            return false;

        BindPlayer(scenePlayer);
        return HasPlayerBinding();
    }

    private void BindPlayer(Transform playerTransform)
    {
        player = playerTransform;
        agent = player != null ? player.GetComponent<NavMeshAgent>() : null;
        pixieSpawner = player != null ? player.GetComponent<PixieSpawner>() : null;
        pixie = pixieSpawner != null ? pixieSpawner.SpawnedPixie : null;
    }

    private bool HasPlayerBinding()
    {
        return player != null && agent != null;
    }

    private void MovePlayer(Vector3 targetPosition)
    {
        if (!HasPlayerBinding())
            return;

        agent.enabled = false;
        player.position = targetPosition;
        agent.Warp(targetPosition);
        agent.enabled = true;
    }

    private Vector3 ResolveNavMeshPosition(Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            return hit.position;

        return targetPosition;
    }

    private void MovePixieToPlayer()
    {
        pixie = pixieSpawner != null ? pixieSpawner.SpawnedPixie : null;
        if (pixie == null)
            return;

        NavMeshAgent pixieAgent = pixie.GetComponent<NavMeshAgent>();
        if (pixieAgent == null)
            return;

        pixieAgent.enabled = false;
        pixie.position = player.position;
        pixieAgent.Warp(player.position);
        pixieAgent.enabled = true;
    }
}
