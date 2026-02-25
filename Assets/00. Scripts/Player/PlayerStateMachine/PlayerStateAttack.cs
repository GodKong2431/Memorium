using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public class PlayerStateAttack : IPlayerState
{
    private float _attackEndTime;
    private bool _attackInProgress;
    private GameObject _currentAttackEffect;

    private bool IsDelayAttack = false;

    public PlayerStateType Type => PlayerStateType.Attack;

    private Transform enemy;

    public void OnEnter(PlayerStateContext ctx)
    {
        enemy = EnemyTarget.GetTarget(ctx.PlayerTransform.position).transform;

        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
            ctx.Agent.isStopped = true;

        float attackSpeed = ctx.StatPresenter?.PlayerStat?.FinalATKSpeed ?? 1f;
        float delay = attackSpeed > 0f ? 1f / attackSpeed : 0.5f;
        _attackEndTime = Time.time + delay;
        _attackInProgress = true;

        if (ctx.AttackEffectPrefab != null)
        {
            if (_currentAttackEffect != null)
                Object.Destroy(_currentAttackEffect);
            Transform t = ctx.PlayerTransform;
            _currentAttackEffect = Object.Instantiate(ctx.AttackEffectPrefab, t.position + Vector3.up * 1f, Quaternion.identity, t);
        }
    }

    public void OnExit(PlayerStateContext ctx)
    {
    }

    public void OnUpdate(PlayerStateContext ctx)
    {
        if (enemy == null && EnemyRegistry.isEnemyExist == false)
        {
            ctx.RequestState(PlayerStateType.Idle);
            return;
        }
        enemy = EnemyTarget.GetTarget(ctx.PlayerTransform.position).transform;
        float dist = Vector3.Distance(ctx.PlayerTransform.position, enemy.position);

        Vector3 dir = enemy.position - ctx.PlayerTransform.position;

        dir.y = 0f;

        Quaternion targetQuat = Quaternion.LookRotation(dir.normalized, Vector3.up);

        float angle = Quaternion.Angle(ctx.PlayerTransform.rotation, targetQuat);

        float perSec = angle / Mathf.Max(0.0001f, ctx.AngularTime);

        ctx.PlayerTransform.rotation = Quaternion.RotateTowards(
            ctx.PlayerTransform.rotation,
            targetQuat,
            perSec * Time.deltaTime
            );

        // 치명타
        var critmult = CritCheck(ctx.StatPresenter.PlayerStat.FinalCritChance) ? ctx.StatPresenter.PlayerStat.FinalCritMult : 1f;

        ctx.SetCritMult(critmult);

        if (!ctx.playerSkillHandler.AutoCast() && dist <= ctx.AttackRange && !IsDelayAttack)
        {
            if (enemy.TryGetComponent<EnemyStateMachine>(out var target))
            {
                BossChecker(target, ctx);
            }

            IsDelayAttack = true;
        }

        if (_attackInProgress && Time.time >= _attackEndTime)
        {
            IsDelayAttack = false;
            _attackInProgress = false;
            ClearAttackEffect();
            ctx.RequestState(PlayerStateType.Chase);
        }
    }

    private void ClearAttackEffect()
    {
        if (_currentAttackEffect != null)
        {
            Object.Destroy(_currentAttackEffect);
            _currentAttackEffect = null;
        }
    }

    private void BossChecker(EnemyStateMachine target, PlayerStateContext ctx)
    {
        if (target.TryGetComponent<EnemyStatPresenter>(out var statPresenter))
        {
            var normal = ctx.StatPresenter.PlayerStat.GatBasicDamage(ctx.StatPresenter.PlayerStat.FinalNormalDamage, 0);
            var boss = ctx.StatPresenter.PlayerStat.GatBasicDamage(ctx.StatPresenter.PlayerStat.FinalBossDamage, 0);

            Debug.Log(statPresenter.IsBoss);

            var finalDamage = statPresenter.IsBoss ? boss : normal;

            target.TakeDamage(finalDamage * ctx.CurrentCritMult);
        }
    }

    private bool CritCheck(float crit)
    {
        var random = Random.Range(0f, 1f);

        if (random < crit)
        {
            return true;
        }

        return false;
    }

    private static void SetAnimatorTrigger(PlayerStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
