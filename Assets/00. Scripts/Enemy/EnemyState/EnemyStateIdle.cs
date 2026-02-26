using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Idle 상태: 진입 시점 위치에 정지, 대기 애니메이션.
/// (플레이어 사망 시 Attack → Idle로 전환)
/// </summary>
public class EnemyStateIdle : IEnemyState
{
    public EnemyStateType Type => EnemyStateType.Idle;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
            ctx.Agent.ResetPath();
        }
        ctx.SetAnimatorTrigger("Idle");
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        // Idle에서는 전환 요청 없음. (플레이어 리스폰 등이 있으면 여기서 Chase로 전환 가능)
    }

    public void OnExit(EnemyStateContext ctx)
    {
    }
}
