using UnityEngine;
using UnityEngine.AI;

public class PlayerStateMove : IPlayerState
{
    [SerializeField]
    [Tooltip("추적을 위한 목적지 갱신 주기입니다.")]
    private const float DestinationRefreshInterval = 0.25f;
    private float _lastDestinationTime = -1f;

    public PlayerStateType Type => PlayerStateType.Move;

    public void OnEnter(PlayerStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            ctx.Agent.isStopped = false;
        _lastDestinationTime = -DestinationRefreshInterval;
        SetAnimatorTrigger(ctx, "Move");
    }

    public void OnExit(PlayerStateContext ctx)
    {
    }

    public void OnUpdate(PlayerStateContext ctx)
    {
        NavMeshAgent agent = ctx.Agent;

        Transform goal = GameObject.FindAnyObjectByType<Goal>().transform;

        agent.SetDestination(goal.position);

        if (EnemyRegistry.isEnemyExist)
        {
            ctx.RequestState(PlayerStateType.Chase);
            return;
        }

        if (agent.pathPending) return;

        if (agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!agent.hasPath || agent.velocity.sqrMagnitude < 0.01f)
            {
                //ctx.isGoal = true;
                ctx.RequestState(PlayerStateType.Idle);
            }
        }
    }


    private static void SetAnimatorTrigger(PlayerStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
