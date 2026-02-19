using System.Collections.Generic;
using UnityEngine;

public class InfinityMap : MonoBehaviour
{
    //반복할 맵
    [SerializeField] private GameObject[] maps;
    //플레이어와 충돌 시 맵을 이동시키는 트리거
    [SerializeField] private GameObject mapMoveTrigger;
    private List<Renderer> mapsRenderer = new List<Renderer>();

    //현재 트리거가 설치된 맵 인덱스
    [SerializeField] int curMapIndex = 1;
    List<Vector3> originMapPos = new List<Vector3>();
    Vector3 originTriggerPos;
    
    private void Awake()
    {
        
        //if(curMapIndex+1 < maps.Length)
        //    mapMoveTrigger.transform.position = maps[curMapIndex + 1].transform.position;
        //바운드 가져오기 위해 맵 렌더러 미리 모두 가져오기
        for (int i = 0; i < maps.Length; i++)
        {
            mapsRenderer.Add(maps[i].GetComponent<Renderer>());
            //맵 정리 코드
            if (i > 0)
            {
                float plusPosition = mapsRenderer[i].bounds.extents.x + mapsRenderer[i-1].bounds.extents.x;
                maps[i].transform.position = maps[i-1].transform.position + Vector3.right * plusPosition;
            }
            originMapPos.Add(maps[i].transform.position);
        }

        mapMoveTrigger.transform.position = maps[curMapIndex].transform.position;

        originTriggerPos = mapMoveTrigger.transform.position;
        Debug.Log(originTriggerPos);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;
        MapMove();
    }


    public void MapMove()
    {
        Debug.Log("[InfinityMap] 맵 이동 시작");
        //현재 딛고 있는 맵 이전 맵 주소
        int moveMapIndex = curMapIndex - 1;
        if (moveMapIndex < 0)
            moveMapIndex = maps.Length - 1;

        Debug.Log($"[InfinityMap] 이동할 맵 주소 {moveMapIndex}");
        //현재 맵에서 가장 멀리 떨어진 맵 주소(현재 -2)
        int furthestMapIndex = moveMapIndex - 1;
        if (furthestMapIndex < 0)
            furthestMapIndex = maps.Length - 1;
        Debug.Log($"[InfinityMap] 가장 멀리 떨어진 맵 주소 {furthestMapIndex}");
        //이동할 맵 이전 맵은 제일 끝에 있는 맵이므로 해당 맵 정보 가져온 다음 거기에 포지션을 추가
        float plusPosition = mapsRenderer[moveMapIndex].bounds.extents.x + mapsRenderer[furthestMapIndex].bounds.extents.x;
        maps[moveMapIndex].transform.position = maps[furthestMapIndex].transform.position + Vector3.right * plusPosition;
        Debug.Log($"[InfinityMap] 맵 이동 완료");
        //다음 맵으로 트리거 이동
        int nextMapIndex = curMapIndex + 1;
        if ((nextMapIndex >= maps.Length))
            nextMapIndex = 0;
        mapMoveTrigger.transform.position = maps[nextMapIndex].transform.position;
        Debug.Log($"[InfinityMap] 트리거 이동 완료");
        curMapIndex++;
        if (curMapIndex >= maps.Length)
            curMapIndex = 0;
    }

    public void MapReset()
    {
        for (int i = 0; i < maps.Length; i++)
        {
            maps[i].transform.position = originMapPos[i];
        }
        mapMoveTrigger.transform.position = originTriggerPos;
    }
}
