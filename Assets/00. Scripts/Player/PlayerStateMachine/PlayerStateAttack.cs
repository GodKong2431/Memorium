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

        float attackSpeed = ctx.StatPresenter?.Data?.baseAttackSpeed ?? 1f;
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
        float dist = Vector3.Distance(ctx.PlayerTransform.position, enemy.position);

        if (enemy == null && EnemyRegistry.isEnemyExist == false)
        {
            ctx.RequestState(PlayerStateType.Idle);
            return;
        }

        if (ctx.isFirstSkillReady == true && dist <= ctx.FirstSkillRange)
        {
            // 첫번째 스킬
            Debug.Log("첫번째 스킬 사용됨");
            ctx.isFirstSkillReady = false;
            Debug.Log("첫번째 스킬 쿨타임 작동");
        }

        else if (ctx.isSecondSkillReady == true && dist <= ctx.SecondSkillRange)
        {
            // 두번째 스킬
            Debug.Log("두번째 스킬 사용됨");
            ctx.isSecondSkillReady = false;
            Debug.Log("두번째 스킬 쿨타임 작동");
        }

        else if (ctx.isThirdSkillReady == true && dist <= ctx.ThirdSkillRange)
        {
            // 세번째 스킬
            Debug.Log("세번째 스킬 사용됨");
            ctx.isThirdSkillReady = false;
            Debug.Log("세번째 스킬 쿨타임 작동");
        }

        else if (dist <= ctx.AttackRange)
        {
            // 일반 공격
            Debug.Log("일반 공격 중");
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
