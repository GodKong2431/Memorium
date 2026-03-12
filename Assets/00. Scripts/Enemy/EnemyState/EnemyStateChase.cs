using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Chase 상태: 플레이어 위치로 NavMesh 이동, 추적 애니메이션.
/// (구 EnemyNavChase 추격 로직 병합: destination 갱신 간격 0.25초, 사거리 내 시 Attack 전환.)
/// </summary>
public class EnemyStateChase : IEnemyState
{
    [SerializeField][Tooltip("추적을 위한 목적지 갱신 주기입니다.")]
    private const float DestinationRefreshInterval = 0.25f;
    private float _lastDestinationTime = -1f;

    public EnemyStateType Type => EnemyStateType.Chase;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.isStopped = false;
        _lastDestinationTime = -DestinationRefreshInterval;
        ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Chase);
        // 이동/발소리 효과음 추가 예정 (선택)
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        Transform player = ctx.PlayerTransform;
        if (player == null) return;

        // 해당 객체는 스테이지에 소환된 직후 플레이어와의 거리가 공격 사거리보다 클 동안 플레이어 위치로 이동
        // 플레이어와의 거리가 공격 사거리 이하(dist <= AttackRange)가 되는 시점에 전투 상태로 전환

        float dist = Vector3.Distance(ctx.EnemyTransform.position, player.position);
        // 거리가 공격 사거리 이하일 때 Attack 상태로 전환
        if (dist <= ctx.AttackRange)
        {
            ctx.RequestState(EnemyStateType.Attack);
            return;
        }

        // 이 부분에 버프/디버프 적용된 이동속도 반영하도록 추가했습니다.
        ctx.Agent.speed = ctx.MoveSpeed;

        NavMeshAgent agent = ctx.Agent;
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh && !agent.isStopped)
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
}
