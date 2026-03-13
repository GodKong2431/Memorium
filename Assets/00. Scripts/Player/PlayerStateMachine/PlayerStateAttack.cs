using UnityEngine;

[System.Serializable]
public class PlayerStateAttack : IPlayerState
{
    private GameObject _currentAttackEffect;
    public PlayerStateType Type => PlayerStateType.Attack;

    [SerializeField] private Transform enemy;

    public void OnEnter(PlayerStateContext ctx)
    {
        enemy = EnemyTarget.GetTarget(ctx.PlayerTransform.position).transform;

        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled)
        {
            ctx.Agent.isStopped = true;
            ctx.Agent.velocity = Vector3.zero;
        }

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
        if (EnemyRegistry.isEnemyExist == false)
        {
            ctx.RequestState(PlayerStateType.Idle);
            return;
        }
        
        enemy = EnemyTarget.GetTarget(ctx.PlayerTransform.position)?.transform;
        
        if (enemy == null)
        {
            ctx.RequestState(PlayerStateType.Idle);
            return;
        }

        float dist = Vector3.Distance(ctx.PlayerTransform.position, enemy.position);
        
        if (!ctx.playerSkillHandler.ReadySkill(dist) && dist > ctx.AttackRange)
        {
            ctx.RequestState(PlayerStateType.Chase);
            return;
        }
        
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
        var critmult = CritCheck(ctx.StatPresenter.PlayerStat.FinalStats[StatType.CRIT_CHANCE].finalStat) ? ctx.StatPresenter.PlayerStat.FinalStats[StatType.CRIT_MULT].finalStat : 1f;

        ctx.SetCritMult(critmult);
    
        float attackSpeed = ctx.StatPresenter?.PlayerStat?.FinalStats[StatType.ATK_SPEED].finalStat ?? 1f;
        float delay = attackSpeed > 0f ? 1f / attackSpeed : 0.5f;
        
    
        if (!ctx.playerSkillHandler.AutoCast() && dist <= ctx.AttackRange && Time.time >= ctx.NextAttackTime)
        {
            ctx.NextAttackTime = Time.time + delay;
            
            if (enemy.TryGetComponent<EnemyStateMachine>(out var target))
            {
                BossChecker(target, ctx);
            }
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
            var normal = ctx.StatPresenter.PlayerStat.GatBasicDamage(ctx.StatPresenter.PlayerStat.FinalStats[StatType.NORMAL_DMG].finalStat, 0);
            var boss = ctx.StatPresenter.PlayerStat.GatBasicDamage(ctx.StatPresenter.PlayerStat.FinalStats[StatType.BOSS_DMG].finalStat, 0);



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