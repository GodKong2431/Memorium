using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AcrossMap : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    private void Awake()
    {
        agent=GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        //에이전트가 링크 위에 있는가
        if (agent.isOnOffMeshLink)
        {
            OffMeshLinkData data = agent.currentOffMeshLinkData;

            //링크의 도착점 계산
            Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;

            //도착점까지 본래 에이전트 속도로 이동
            agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);

            //도착점 도달 시 링크를 종료
            if (agent.transform.position == endPos)
            {
                agent.CompleteOffMeshLink();
            }
        }
    }
}
