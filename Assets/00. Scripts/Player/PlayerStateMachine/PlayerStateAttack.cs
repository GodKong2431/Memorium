using UnityEngine;

public class PlayerStateAttack : IPlayerState
{
    private float _attackEndTime;
    private bool _attackInProgress;
    private GameObject _currentAttackEffect;

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



        if (!ctx.playerSkillHandler.AutoCast() && dist <= ctx.AttackRange)
        {
            if (enemy.TryGetComponent<EnemyStateMachine>(out var target))
            {
                target.TakeDamage(ctx.StatPresenter.PlayerStat.FinalATK);
            }
        }


        if (_attackInProgress && Time.time >= _attackEndTime)
        {
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

    private static void SetAnimatorTrigger(PlayerStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
