using UnityEngine;
using UnityEngine.AI;

public class PlayerStateChase : IPlayerState
{
    [SerializeField]
    [Tooltip("추적을 위한 목적지 갱신 주기입니다.")]
    private const float DestinationRefreshInterval = 0.25f;
    private float _lastDestinationTime = -1f;

    public PlayerStateType Type => PlayerStateType.Chase;

    public void OnEnter(PlayerStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            ctx.Agent.isStopped = false;
        _lastDestinationTime = -DestinationRefreshInterval;
        SetAnimatorTrigger(ctx, "Chase");
    }

    public void OnExit(PlayerStateContext ctx)
    {
    }

    public void OnUpdate(PlayerStateContext ctx)
    {
        if (!EnemyRegistry.isEnemyExist)
        {
            ctx.RequestState(PlayerStateType.Idle);
            return;
        }

        var target = EnemyTarget.GetTarget(ctx.PlayerTransform.position).transform;
        if (target == null)
        {
            return;
        }

        Transform enemy = target.transform;
        // 해당 객체는 스테이지에 소환된 직후 플레이어와의 거리가 공격 사거리보다 클 동안 플레이어 위치로 이동
        // 플레이어와의 거리가 공격 사거리 이하(dist <= AttackRange)가 되는 시점에 전투 상태로 전환

        float dist = Vector3.Distance(ctx.PlayerTransform.position, enemy.position);
        // 거리가 공격 사거리 이하일 때 Attack 상태로 전환

        //if (ctx.playerSkillHandler.ReadySkill(dist) || dist <= ctx.AttackRange)
        //{
        //    ctx.RequestState(PlayerStateType.Attack);
        //    return;
        //}

        if (dist <= ctx.AttackRange)
        {
            ctx.RequestState(PlayerStateType.Attack);
            return;
        }

        NavMeshAgent agent = ctx.Agent;
        if (agent != null && agent.isActiveAndEnabled && !agent.isStopped)
        {
            if (Time.time - _lastDestinationTime >= DestinationRefreshInterval)
            {
                _lastDestinationTime = Time.time;
                agent.SetDestination(enemy.position);
            }
        }
    }
    private static void SetAnimatorTrigger(PlayerStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
