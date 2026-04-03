using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Onhit 상태: Attack 중 피격 시 진입. 피격 애니메이션·이펙트 후 Chase로 복귀.
/// </summary>
public class EnemyStateOnhit : IEnemyState
{
    private const float OnhitDuration = 0.4f;
    public EnemyStateType Type => EnemyStateType.Onhit;

    public void OnEnter(EnemyStateContext ctx)
    {
        var st = ctx.Instance;
        st.OnhitKnockbackActive = false;
        st.OnhitKnockbackElapsed = 0f;
        st.OnhitKnockbackSpeed = 0f;
        st.OnhitKnockbackInfo = default;

        st.OnhitEndTime = Time.time + OnhitDuration;
        ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Onhit);
        if (ctx.EnemyTransform != null && ctx.OnHitEffectPrefab != null)
        {
            if (st.CurrentOnhitEffect != null)
                EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentOnhitEffect);

            var prefab = ctx.OnHitEffectPrefab;
            var parent = ctx.EnemyTransform;
            var go = ObjectPoolManager.Get(prefab, parent.position, parent.rotation, parent);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = prefab.transform.localScale;
            st.CurrentOnhitEffect = go;
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
        ProcessKnockback(ctx);

        var st = ctx.Instance;
        if (Time.time >= st.OnhitEndTime && (!st.OnhitKnockbackActive || st.OnhitKnockbackElapsed >= st.OnhitKnockbackInfo.duration))
        {
            ctx.RequestState(EnemyStateType.Chase);
        }
    }

    public void OnExit(EnemyStateContext ctx)
    {
        ExitKnockback(ctx);
        var st = ctx?.Instance;
        if (st?.CurrentOnhitEffect != null)
        {
            EnemyStateMachine.DestroyOrReturnPooledEffect(st.CurrentOnhitEffect);
            st.CurrentOnhitEffect = null;
        }
    }

    #region 넉백 

    private void InitKnockback(EnemyStateContext ctx)
    {
        var st = ctx.Instance;
        if (ctx.PendingKnockback.HasValue)
        {
            if (ctx.IsBoss)
            {
                ctx.PendingKnockback = null;
                st.OnhitKnockbackActive = false;
                return;
            }

            st.OnhitKnockbackActive = true;
            st.OnhitKnockbackInfo = ctx.PendingKnockback.Value;
            ctx.PendingKnockback = null;

            st.OnhitKnockbackSpeed = st.OnhitKnockbackInfo.distance / st.OnhitKnockbackInfo.duration;
            st.OnhitKnockbackElapsed = 0f;


            float knockbackEndTime = Time.time + st.OnhitKnockbackInfo.duration;
            if (knockbackEndTime > st.OnhitEndTime)
            {
                st.OnhitEndTime = knockbackEndTime;
            }
            if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            {
                ctx.Agent.enabled = false;
            }
        }
        else
        {
            st.OnhitKnockbackActive = false;
        }
    }

    private void ProcessKnockback(EnemyStateContext ctx)
    {
        var st = ctx.Instance;
        if (!st.OnhitKnockbackActive) return;

        if (st.OnhitKnockbackElapsed < st.OnhitKnockbackInfo.duration)
        {
            st.OnhitKnockbackElapsed += Time.deltaTime;

            Vector3 direction = st.OnhitKnockbackInfo.direction;
            direction.y = 0f;
            direction = direction.normalized;

            float step = st.OnhitKnockbackSpeed * Time.deltaTime;
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

        var st = ctx.Instance;
        if (!st.OnhitKnockbackActive) return;
        st.OnhitKnockbackActive = false;

        if (ctx.Agent ==null) return;

        ctx.Agent.enabled = true;
        ctx.Agent.Warp(ctx.EnemyTransform.position);
        ctx.PendingKnockback = null;
    }

    #endregion
}
