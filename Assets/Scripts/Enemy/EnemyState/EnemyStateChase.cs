using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Chase 상태: 플레이어 위치로 NavMesh 이동, 추적 애니메이션.
/// (구 EnemyNavChase 추격 로직 병합: destination 갱신 간격 0.25초, 사거리 내 시 Attack 전환.)
/// </summary>
public class EnemyStateChase : IEnemyState
{
    private const float DestinationRefreshInterval = 0.25f;
    private float _lastDestinationTime = -1f;

    public EnemyStateType Type => EnemyStateType.Chase;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            ctx.Agent.isStopped = false;
        _lastDestinationTime = -DestinationRefreshInterval;
        SetAnimatorTrigger(ctx, "Chase");
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        Transform player = ctx.PlayerTransform;
        if (player == null) return;

        float dist = Vector3.Distance(ctx.EnemyTransform.position, player.position);
        if (dist <= ctx.AttackRange)
        {
            ctx.RequestState(EnemyStateType.Attack);
            return;
        }

        NavMeshAgent agent = ctx.Agent;
        if (agent != null && agent.isActiveAndEnabled && !agent.isStopped)
        {
            if (Time.time - _lastDestinationTime >= DestinationRefreshInterval)
            {
                _lastDestinationTime = Time.time;
                agent.SetDestination(player.position);
            }
        }
    }

    public void OnExit(EnemyStateContext ctx)
    {
    }

    private static void SetAnimatorTrigger(EnemyStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
