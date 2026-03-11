using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AcrossMap : MonoBehaviour
{
    [SerializeField] NavMeshAgent agent;
    Rigidbody rb;
    bool isRbKinematic = false;
    
    [SerializeField] private float currentLinkSpeed;

    private void Awake()
    {
        agent=GetComponent<NavMeshAgent>();
        rb=GetComponent<Rigidbody>();
    }

    void Update()
    {
        //에이전트가 링크 위에 있는가
        if (agent.isOnOffMeshLink)
        {
            if (!isRbKinematic)
            {
                agent.velocity = Vector3.zero;
                agent.nextPosition = transform.position;

                currentLinkSpeed = agent.velocity.magnitude;

                rb.isKinematic = true;
                isRbKinematic = true;

                agent.updatePosition = false;
                agent.updateRotation = false;
            }
            OffMeshLinkData data = agent.currentOffMeshLinkData;

            //링크의 도착점 계산
            Vector3 endPos = data.endPos + Vector3.up * agent.baseOffset;
            endPos.y = transform.position.y;

            //도착점까지 본래 에이전트 속도로 이동
            //agent.transform.position = Vector3.MoveTowards(agent.transform.position, endPos, agent.speed * Time.deltaTime);
            //transform.position = Vector3.MoveTowards(transform.position, endPos, agent.speed * Time.deltaTime);
            transform.position = Vector3.MoveTowards(transform.position, endPos, currentLinkSpeed * Time.deltaTime);

            agent.nextPosition = transform.position;
            //도착점 도달 시 링크를 종료
            //if (agent.transform.position == endPos)
            if (Vector3.Distance(transform.position, endPos) < 0.05f)
            {
                agent.CompleteOffMeshLink();
                rb.isKinematic = false;
                isRbKinematic = false;
                agent.updatePosition = true;
                agent.updateRotation = true;
            }
        }
    }
}
