using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InfinityMap : MonoBehaviour
{
    [SerializeField] private GameObject mapMoveTrigger;
    [SerializeField] private int curMapIndex = 1;
    [SerializeField] private Vector3 originPlayerPos;
    [SerializeField] private Transform goal;
    [SerializeField] private Transform player;
    [SerializeField] private Transform pixie;
    [SerializeField] private GameObject[] mapGroupsPrefab;

    private readonly List<Renderer> mapsRenderer = new List<Renderer>();
    private readonly List<Vector3> originMapPos = new List<Vector3>();

    private Vector3 originTriggerPos;
    private NavMeshAgent agent;
    private PixieSpawner pixieSpawner;

    public List<GameObject> mapGroups;
    public List<GameObject> maps;
    public bool firstMapSetting = false;

    private void OnEnable()
    {
        GameEventManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        GameEventManager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => MapManager.Instance != null);
        yield return new WaitUntil(() => MapManager.Instance.mapSetting);

        maps = MapManager.Instance.maps;

        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance.infinityMap = this;

        MapInit();
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

        goal.SetParent(maps[maps.Count - 1].transform);
        goal.transform.localPosition = Vector3.zero;

        mapMoveTrigger.transform.position = maps[curMapIndex].transform.position;
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

        float plusPosition = mapsRenderer[moveMapIndex].bounds.extents.x + mapsRenderer[furthestMapIndex].bounds.extents.x;
        maps[moveMapIndex].transform.position = maps[furthestMapIndex].transform.position + Vector3.right * plusPosition;

        int nextMapIndex = curMapIndex + 1;
        if (nextMapIndex >= maps.Count)
            nextMapIndex = 0;

        mapMoveTrigger.transform.position = maps[nextMapIndex].transform.position;

        curMapIndex++;
        if (curMapIndex >= maps.Count)
            curMapIndex = 0;

        goal.transform.SetParent(maps[moveMapIndex].transform);
        goal.transform.localPosition = Vector3.zero;
    }

    public void MapReset()
    {
        for (int i = 0; i < maps.Count; i++)
            maps[i].transform.position = originMapPos[i];

        mapMoveTrigger.transform.position = originTriggerPos;
        curMapIndex = 1;

        goal.SetParent(maps[maps.Count - 1].transform);
        goal.transform.localPosition = Vector3.zero;

        PlayerPosInit();
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
