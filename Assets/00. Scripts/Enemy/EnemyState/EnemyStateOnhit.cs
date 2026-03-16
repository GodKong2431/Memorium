using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Onhit 상태: Attack 중 피격 시 진입. 피격 애니메이션·이펙트 후 Chase로 복귀.
/// </summary>
public class EnemyStateOnhit : IEnemyState
{
    private const float OnhitDuration = 0.4f;
    private float _endTime;

    private KnockbackInfo knockbackInfo;
    private float knockbackSpeed;
    private float elapsedTime;
    public EnemyStateType Type => EnemyStateType.Onhit;

    public void OnEnter(EnemyStateContext ctx)
    {
        _endTime = Time.time + OnhitDuration;
        ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Onhit);
        // 피격 이펙트 추가 예정
        if (ctx.OnHitEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(ctx.OnHitEffectPrefab, ctx.EnemyTransform.position, Quaternion.identity);
            Object.Destroy(effect, 1.0f);
        }
        // 피격 효과음 추가 예정
        InitKnockback(ctx);
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (ctx.CurrentHealth <= 0f)
        {
            ctx.RequestState(EnemyStateType.Dead);
            return;
        }
        ProcessKnockback(ctx);

        if (Time.time >= _endTime)
            ctx.RequestState(EnemyStateType.Chase);
    }

    public void OnExit(EnemyStateContext ctx)
    {
        ExitKnockback(ctx);
    }

    #region 넉백 

    private void InitKnockback(EnemyStateContext ctx)
    {
        if (ctx.PendingKnockback.HasValue)
        {
            knockbackInfo = ctx.PendingKnockback.Value;

            knockbackSpeed = knockbackInfo.distance / knockbackInfo.duration;
            elapsedTime = 0f;

            _endTime = Time.time + knockbackInfo.duration;

            if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            {
                ctx.Agent.enabled = false;
            }
        }
    }

    private void ProcessKnockback(EnemyStateContext ctx)
    {
        if (ctx.PendingKnockback.HasValue) return;

        if (elapsedTime < knockbackInfo.duration)
        {
            elapsedTime += Time.deltaTime;

            Vector3 direction = knockbackInfo.direction;
            direction.y = 0f;
            direction = direction.normalized;

            float step = knockbackSpeed * Time.deltaTime;
            Vector3 nextPos = ctx.EnemyTransform.position + direction * step;

            if (NavMesh.SamplePosition(nextPos, out var hit, 2.0f, NavMesh.AllAreas))
            {
                nextPos = hit.position;
            }

            nextPos.y = ctx.EnemyTransform.position.y;
            ctx.EnemyTransform.position = nextPos;
        }
    }

    private void ExitKnockback(EnemyStateContext ctx)
    {

        if (!ctx.PendingKnockback.HasValue)  return; 
        if (ctx.Agent ==null) return;

        ctx.Agent.enabled = true;
        ctx.Agent.Warp(ctx.EnemyTransform.position);
        ctx.PendingKnockback = null;
    }

    #endregion
}
