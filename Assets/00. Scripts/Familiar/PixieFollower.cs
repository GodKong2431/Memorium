using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PixieEffectProvider))]
public class PixieFollower :MonoBehaviour
{
    [SerializeField] private float followDelay = 0.25f;
    [SerializeField] private float teleportDistance = 1000f;
    [SerializeField] private float stoppingDistance = 3.0f;

    private NavMeshAgent agent;
    private Transform followTarget;
    private float lastUpdateTime;
    private PlayerStateContext stateContext;

    private OwnedFairyData fairyData;
    public OwnedFairyData FairyData => fairyData;

    private void Update()
    {
        if (followTarget == null || agent == null) return;
        if (!agent.isOnNavMesh) return;

        float dist = GetDistance();

        if (dist >= teleportDistance)
        {
            Warp();
            return;
        }
        if (dist <= stoppingDistance)   return;
        if (Time.time - lastUpdateTime >= followDelay)
        {
            lastUpdateTime = Time.time;
            agent.SetDestination(followTarget.position);
        }
    }
    public void Init(Transform target, OwnedFairyData data, EffectController playerEffectController)
    {
        this.followTarget = target;
        this.fairyData = data;
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = stoppingDistance;
        agent.speed = CharacterStatManager.Instance.GetFinalStat(StatType.MOVE_SPEED) * 1.2f; 

        var effectProvider = GetComponent<PixieEffectProvider>();
        effectProvider.Init(data, target, playerEffectController, stateContext);
        Warp();
        

    }
    
    public float GetDistance()
    {
        var position = transform.position;
        var targetPosition = followTarget.position;
        position.y = 0;
        targetPosition.y = 0;
        var distance = Vector3.Distance(position, targetPosition);
        return distance;
    }

    public void Warp()
    {
        agent.Warp(followTarget.position);
        agent.ResetPath();
        lastUpdateTime = Time.time;
    }
}