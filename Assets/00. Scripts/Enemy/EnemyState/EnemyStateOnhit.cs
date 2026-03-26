using UnityEngine;
using UnityEngine.AI;

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
        if (ctx.OnHitEffectPrefab != null)
        {
            GameObject effect = Object.Instantiate(ctx.OnHitEffectPrefab, ctx.EnemyTransform.position, Quaternion.identity);
            Object.Destroy(effect, 1.0f);
        }

        if (ctx.OnHitSoundId > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayCombatSfxAt(ctx.OnHitSoundId, ctx.EnemyTransform.position);

        InitKnockback(ctx);
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (ctx.CurrentHealth <= 0f)
        {
            ctx.RequestState(EnemyStateType.Dead);
            return;
        }

        if (Time.time >= _endTime)
        {
            ctx.RequestState(EnemyStateType.Chase);
        }
    }

    public void OnExit(EnemyStateContext ctx)
    {
       
    }

}
