using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 몬스터 Attack 상태: 공격 애니메이션 및 이펙트 표시
/// 스킬 공격형: SkillCaster로 스킬 시전 (예: 어스 위저드 - 지면에서 암석 소환)
/// 일반형: 근접 공격 애니메이션 + 이펙트
/// 플레이어 사망 시 Idle, 몬스터 사망 시 Dead, 피격 시 Onhit으로 전환.
/// 공격 타이밍에 플레이어가 사거리 내에 있으면 TakeDamage 호출.
/// </summary>
public class EnemyStateAttack : IEnemyState
{
    private float _attackEndTime;
    private bool _attackInProgress;
    private GameObject _currentAttackEffect;
    private bool _isSkillAttack;
    private bool _damageApplied;

    public EnemyStateType Type => EnemyStateType.Attack;

    public void OnEnter(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.isStopped = true;

        _isSkillAttack = ctx.IsSkillAttackType;

        if (_isSkillAttack && ctx.SkillHandler != null)
        {
            if (ctx.SkillHandler.TryCastSkill())
            {
                _attackInProgress = true;
                _attackEndTime = float.MaxValue;
                SetAnimatorTrigger(ctx, "Attack");
            }
            else
            {
                ctx.RequestState(EnemyStateType.Chase);
                return;
            }
        }
        else
        {
            _damageApplied = false;
            float attackSpeed = ctx.StatPresenter?.Data?.monsterAttackspeed ?? 1f;
            float delay = attackSpeed > 0f ? 1f / attackSpeed : 0.5f;
            _attackEndTime = Time.time + delay;
            _attackInProgress = true;

            if (ctx.IsBoss)
                SetAnimatorTrigger(ctx, "AttackBoss");
            else
                SetAnimatorTrigger(ctx, "Attack");

            if (ctx.AttackEffectPrefab != null)
            {
                if (_currentAttackEffect != null)
                    Object.Destroy(_currentAttackEffect);
                Transform t = ctx.EnemyTransform;
                _currentAttackEffect = Object.Instantiate(ctx.AttackEffectPrefab, t.position + Vector3.up * 1f, Quaternion.identity, t);
            }
        }
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        if (!ctx.IsPlayerAlive())
        {
            ctx.RequestState(EnemyStateType.Idle);
            return;
        }

        if (ctx.CurrentHealth <= 0f)
        {
            ctx.RequestState(EnemyStateType.Dead);
            return;
        }

        if (_isSkillAttack)
        {
            if (!ctx.SkillHandler.IsCasting)
            {
                ctx.RequestState(EnemyStateType.Chase);
            }
        }
        else if (_attackInProgress && Time.time >= _attackEndTime)
        {
            // 공격 타이밍에 플레이어가 사거리 내에 있으면 TakeDamage 호출
            if (!_damageApplied && ctx.PlayerTransform != null)
            {
                float dist = Vector3.Distance(ctx.EnemyTransform.position, ctx.PlayerTransform.position);
                if (dist <= ctx.AttackRange)
                {
                    var damageable = ctx.PlayerTransform.GetComponent<IDamageable>();
                    if (damageable != null)
                    {
                        float damage = ctx.StatPresenter?.Data?.monsterAttackpoint ?? 10f;
                        if (ctx.SkillHandler != null)
                            damage = ctx.SkillHandler.GetAttack();
                        Debug.Log($"[EnemyStateAttack] damage={damage}");
                        damageable.TakeDamage(damage, DamageType.Physical);
                        _damageApplied = true;
                    }
                }
            }
            _attackInProgress = false;
            ClearAttackEffect();
            ctx.RequestState(EnemyStateType.Chase);
        }
    }

    public void OnExit(EnemyStateContext ctx)
    {
        ClearAttackEffect();
    }

    private void ClearAttackEffect()
    {
        if (_currentAttackEffect != null)
        {
            Object.Destroy(_currentAttackEffect);
            _currentAttackEffect = null;
        }
    }

    private static void SetAnimatorTrigger(EnemyStateContext ctx, string trigger)
    {
        if (ctx.Animator != null && !string.IsNullOrEmpty(trigger))
            ctx.Animator.SetTrigger(trigger);
    }
}
