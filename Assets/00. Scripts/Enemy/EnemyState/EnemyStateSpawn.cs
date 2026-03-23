using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 보스 전용 상태: 스폰 연출 애니메이션 재생 후 Chase로 전환.
/// 일반 몬스터는 사용하지 않음.
/// </summary>
public class EnemyStateSpawn : IEnemyState
{
    private const float SpawnDuration = 2f;
    private float _endTime;

    [Header("Spawn 동안 NavMeshAgent 정지 여부")]
    [Tooltip("기본 false: 스폰 중에도 agent가 멈춰있는 상태가 남지 않게 처리")]
    [SerializeField] private bool stopAgentDuringSpawn = false;

    public EnemyStateType Type => EnemyStateType.Spawn;

    public void OnEnter(EnemyStateContext ctx)
    {
        // 풀링으로 agent.isStopped 값이 남아있을 수 있으니, 스폰 진입 시 초기화
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.isStopped = false;

        if (stopAgentDuringSpawn && ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.isStopped = true;

        ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Spawn);
        _endTime = Time.time + SpawnDuration;
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (ctx.CurrentHealth <= 0f)
        {
            ctx.RequestState(EnemyStateType.Dead);
            return;
        }

        if (Time.time >= _endTime)
            ctx.RequestState(EnemyStateType.Chase);
    }

    public void OnExit(EnemyStateContext ctx)
    {
    }
}
