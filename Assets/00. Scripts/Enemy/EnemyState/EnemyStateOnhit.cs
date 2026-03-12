using UnityEngine;

/// <summary>
/// 몬스터 Onhit 상태: Attack 중 피격 시 진입. 피격 애니메이션·이펙트 후 Chase로 복귀.
/// </summary>
public class EnemyStateOnhit : IEnemyState
{
    private const float OnhitDuration = 0.4f;
    private float _endTime;

    public EnemyStateType Type => EnemyStateType.Onhit;

    public void OnEnter(EnemyStateContext ctx)
    {
        _endTime = Time.time + OnhitDuration;
        ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Onhit);
        // 피격 이펙트 추가 예정
        // 피격 효과음 추가 예정
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
