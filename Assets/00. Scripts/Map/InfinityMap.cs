using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class InfinityMap : MonoBehaviour
{ 

    //플레이어와 충돌 시 맵을 이동시키는 트리거
    [SerializeField] private GameObject mapMoveTrigger;
    private List<Renderer> mapsRenderer = new List<Renderer>();

    //현재 트리거가 설치된 맵 인덱스
    [SerializeField] int curMapIndex = 1;
    List<Vector3> originMapPos = new List<Vector3>();
    Vector3 originTriggerPos;
    [SerializeField] Vector3 originPlayerPos;

    //목표 위치
    [SerializeField] Transform goal;

    //플레이어
    [SerializeField] Transform player;
    [SerializeField] Transform pixie;

    [SerializeField] private GameObject[] mapGroupsPrefab;

    public List<GameObject> mapGroups;
    public List<GameObject> maps;
    public bool firstMapSetting=false;

    NavMeshAgent agent;
    PixieSpawner pixieSpawner;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => MapManager.Instance != null);
        yield return new WaitUntil(() => MapManager.Instance.mapSetting);
        
        //매니저에 있는 맵 참조<- 주소를 참조하여 이후에도 동기화 진행
        maps=MapManager.Instance.maps;
        yield return new WaitUntil(() => StageManager.Instance != null);
        StageManager.Instance.infinityMap = this;

        MapInit();
    }

    public void MapInit()
    {
        mapsRenderer.Clear();
        curMapIndex = 1;
        //바운드 가져오기 위해 맵 렌더러 미리 모두 가져오기
        for (int i = 0; i < maps.Count; i++)
        {
            mapsRenderer.Add(maps[i].GetComponent<Renderer>());
            //맵 정리 코드
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

        PlayerPosInit();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (mapsRenderer == null || mapsRenderer.Count == 0)
            return;
        if (!other.CompareTag("Player"))
            return;
        MapMove();
    }

    public void MapMove()
    {
        //현재 딛고 있는 맵 이전 맵 주소
        int moveMapIndex = curMapIndex - 1;
        if (moveMapIndex < 0)
            moveMapIndex = maps.Count - 1;

        //Debug.Log($"[InfinityMap] 이동할 맵 주소 {moveMapIndex}");
        //현재 맵에서 가장 멀리 떨어진 맵 주소(현재 -2)
        int furthestMapIndex = moveMapIndex - 1;
        if (furthestMapIndex < 0)
            furthestMapIndex = maps.Count - 1;
        //Debug.Log($"[InfinityMap] 가장 멀리 떨어진 맵 주소 {furthestMapIndex}");
        //이동할 맵 이전 맵은 제일 끝에 있는 맵이므로 해당 맵 정보 가져온 다음 거기에 포지션을 추가
        float plusPosition = mapsRenderer[moveMapIndex].bounds.extents.x + mapsRenderer[furthestMapIndex].bounds.extents.x;
        maps[moveMapIndex].transform.position = maps[furthestMapIndex].transform.position + Vector3.right * plusPosition;
        //Debug.Log($"[InfinityMap] 맵 이동 완료");
        //다음 맵으로 트리거 이동
        int nextMapIndex = curMapIndex + 1;
        if ((nextMapIndex >= maps.Count))
            nextMapIndex = 0;
        mapMoveTrigger.transform.position = maps[nextMapIndex].transform.position;
        //Debug.Log($"[InfinityMap] 트리거 이동 완료");
        curMapIndex++;
        if (curMapIndex >= maps.Count)
            curMapIndex = 0;

        goal.transform.SetParent(maps[moveMapIndex].transform);
        goal.transform.localPosition = Vector3.zero;
    }

    public void MapReset()
    {
        for (int i = 0; i < maps.Count; i++)
        {
            maps[i].transform.position = originMapPos[i];
        }
        mapMoveTrigger.transform.position = originTriggerPos;
        curMapIndex = 1;

        goal.SetParent(maps[maps.Count - 1].transform);
        goal.transform.localPosition = Vector3.zero;

        PlayerPosInit();
    }

    public void PlayerPosInit()
    {
        if (player == null)
        {
            player = GameObject.FindAnyObjectByType<PlayerStatPresenter>().transform;
            
            originPlayerPos = player.position;

            if (player != null)
            {
                agent = player.GetComponent<NavMeshAgent>();
                NavMeshHit hit;
                agent.enabled = false;
                if (NavMesh.SamplePosition(player.position, out hit, 1.0f, NavMesh.AllAreas))
                {
                    player.position = hit.position;
                    agent.Warp(hit.position);
                }
                agent.enabled = true;

                pixieSpawner = player.GetComponent<PixieSpawner>();
                originPlayerPos = player.position;
            }
        }
        else
        {
            agent.enabled = false;
            player.position = originPlayerPos;
            agent.Warp(originPlayerPos);
            agent.enabled = true;

            pixie = pixieSpawner.SpawnedPixie;
            if (pixie != null)
            {
                //픽시는 중간에 바뀐 가능성이 있어 매번 가져옴
                NavMeshAgent pixieAgent = pixie.GetComponent<NavMeshAgent>();
                pixieAgent.enabled = false;
                Debug.Log("[InfinityMap] 픽시 이동");
                pixie.position = player.position;
                pixieAgent.Warp(player.position);
                pixieAgent.enabled = true;
            }
            else
            {
                Debug.Log("[InfinityMap] 픽시가 없다");
            }
                
        }
    }
}