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

        // 일반 몬스터(근접/원거리 포함): 기존 로직 그대로 사용
        if (!_isSkillAttack)
        {
            _damageApplied = false;

            float attackSpeed = ctx.AttackSpeed;
            float delay = attackSpeed > 0f ? 1f / attackSpeed : 0.5f;
            _attackEndTime = Time.time + delay;
            _attackInProgress = true;

            if (ctx.IsBoss)
                ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.AttackBoss);
            else
                ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Attack);

            if (ctx.AttackEffectPrefab != null)
            {
                if (_currentAttackEffect != null)
                    Object.Destroy(_currentAttackEffect);
                Transform t = ctx.EnemyTransform;
                _currentAttackEffect = Object.Instantiate(ctx.AttackEffectPrefab, t.position + Vector3.up * 1f, Quaternion.identity, t);
            }
            return;
        }

        // 보스: BossManageTable 기반 스킬 공격
        if (ctx.BossAttackManager == null)
        {
            Debug.LogWarning("[EnemyStateAttack] BossAttackManager 없음 → Chase로 복귀");
            ctx.RequestState(EnemyStateType.Chase);
            return;
        }

        var bossAttack = ctx.BossAttackManager.SelectNextAttack();
        if (bossAttack == null)
        {
            Debug.LogWarning("[EnemyStateAttack] Boss 공격 선택 실패(bossAttack == null) → Chase로 복귀");
            ctx.RequestState(EnemyStateType.Chase);
            return;
        }

        _damageApplied = false;
        _attackInProgress = true;
        _attackEndTime = Time.time + bossAttack.castingDelay + bossAttack.castingTime;
        // Debug.Log($"[EnemyStateAttack] 보스 공격 시작 - enemy={ctx.EnemyTransform.name}, attackId={bossAttack.ID}, type={bossAttack.attackType}, delay={bossAttack.castingDelay}, cast={bossAttack.castingTime}");

        // 애니메이션 트리거는 BossManageTable.animation 값을 그대로 사용한다고 가정
        if (!string.IsNullOrEmpty(bossAttack.animation))
            ctx.SetAnimatorTrigger(bossAttack.animation);
        else
            ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.AttackBoss);

        if (ctx.AttackEffectPrefab != null)
        {
            if (_currentAttackEffect != null)
                Object.Destroy(_currentAttackEffect);
            Transform t = ctx.EnemyTransform;
            _currentAttackEffect = Object.Instantiate(ctx.AttackEffectPrefab, t.position + Vector3.up * 1f, Quaternion.identity, t);
        }
        // BossManageTable.effect 컬럼과 실제 파티클 매핑은 별도 세팅에서 처리
    }

    public void OnUpdate(EnemyStateContext ctx)
    {
        // 풀링 시 파괴된 플레이어 참조 방지 (Unity == null은 파괴된 오브젝트도 처리)
        if (ctx.PlayerTransform == null)
        {
            ctx.RequestState(EnemyStateType.Idle);
            return;
        }
        var playerStateMachine = ctx.PlayerTransform.GetComponent<PlayerStateMachine>();
        if (playerStateMachine == null || playerStateMachine._ctx.CurrentHealth <= 0f)
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
            if (_attackInProgress && Time.time >= _attackEndTime)
            {
                // 보스 스킬 공격: BossManageTable 기반으로 대미지/속성 적용
                if (!_damageApplied && ctx.PlayerTransform != null)
                {
                    float dist = Vector3.Distance(ctx.EnemyTransform.position, ctx.PlayerTransform.position);
                    if (dist <= ctx.AttackRange)
                    {
                        var damageable = ctx.PlayerTransform.GetComponent<IDamageable>();
                        if (damageable != null)
                        {
                            float baseDamage = ctx.AttackPoint;
                            float rate = 1f;
                            DamageType damageType = DamageType.Physical;

                            var current = ctx.BossAttackManager?.CurrentAttack;
                            if (current != null)
                            {
                                rate = current.skillDamageRate;
                                damageType = current.atkAttributeType == AtkAttributeType.magicalAttack
                                    ? DamageType.Magic
                                    : DamageType.Physical;
                            }

                            float damage = baseDamage * rate;
                            // Debug.Log($"[EnemyStateAttack] 보스 공격 히트 - enemy={ctx.EnemyTransform.name}, base={baseDamage}, rate={rate}, final={damage}, type={damageType}");
                            damageable.TakeDamage(damage, damageType);
                            _damageApplied = true;
                        }
                    }
                }

                _attackInProgress = false;
                ClearAttackEffect();
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
                        // 이 부분 버프 계산된 공격력 가져오는걸로 수정했습니다.
                        float damage = ctx.AttackPoint;
                        if (ctx.SkillHandler != null)
                            damage = ctx.SkillHandler.GetAttack();

                        damageable.TakeDamage(damage, DamageType.Physical);
                        _damageApplied = true;
                        // 공격 타격 효과음 추가 예정
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
}