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
    /// <summary>보스 skillAttack2: Animator 서브 SM(Skill2Flow) 진입용 단일 트리거</summary>
    private const string BossSkill2SubStateTrigger = "Animation_Boss_Attack_Skill2";
    private const float SkillAttackEffectLifeTime = 1.5f;

    private float _attackEndTime;
    private bool _attackInProgress;
    private GameObject _currentAttackEffect;
    private bool _isSkillAttack;
    private bool _damageApplied;
    private BossManageTable _currentBossAttack;
    public EnemyStateType Type => EnemyStateType.Attack;

    public void OnEnter(EnemyStateContext ctx)
    {
        // Debug.Log($"[EnemyStateAttack] OnEnter - effect={(ctx.AttackEffectPrefab == null ? "NULL" : ctx.AttackEffectPrefab.name)}");
        
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
        {
            ctx.Agent.isStopped = true;
            ctx.Agent.updateRotation = false;
        }

        _isSkillAttack = ctx.IsSkillAttackType;

        // 일반 몬스터(근접/원거리 포함)
        if (!_isSkillAttack)
        {
            _damageApplied = false;

            float attackSpeed = ctx.AttackSpeed;
            // float delay = attackSpeed > 0f ? 1f / attackSpeed : 0.5f;
            float delay = 0.5f;
            _attackEndTime = Time.time + delay;
            _attackInProgress = true;

            ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Attack);

            // DB에 skillAttackEffectPrefab이 설정된 몹만 플레이어 머리 위에 스킬 연출 VFX를 출력.
            if (ctx.SkillAttackEffectPrefab != null)
                SpawnSkillAttackEffectAtPlayerHead(ctx);
            else
                SpawnAttackEffect(ctx, spawnOnPlayer: true);
            return;
        }

        // 보스: BossManageTable 기반 스킬 공격
        if (ctx.BossAttackManager == null)
        {
            Debug.LogWarning("[EnemyStateAttack] BossAttackManager 없음 → Chase로 복귀");
            ctx.RequestState(EnemyStateType.Chase);
            return;
        }

        _currentBossAttack = ctx.BossAttackManager.SelectNextAttack();
        if (_currentBossAttack == null)
        {
            Debug.LogWarning("[EnemyStateAttack] Boss 공격 선택 실패(bossAttack == null) → Chase로 복귀");
            ctx.RequestState(EnemyStateType.Chase);
            return;
        }

        _damageApplied = false;
        _attackInProgress = true;
        _attackEndTime = Time.time + _currentBossAttack.castingDelay + _currentBossAttack.castingTime;
        // Debug.Log($"[EnemyStateAttack] 보스 공격 시작 - enemy={ctx.EnemyTransform.name}, attackId={_currentBossAttack.ID}, type={_currentBossAttack.attackType}, delay={_currentBossAttack.castingDelay}, cast={_currentBossAttack.castingTime}");

        if (_currentBossAttack.attackType != AttackType.normalAttack)
            PlayBossAreaPrepareSound(ctx);

        if (_currentBossAttack.attackType == AttackType.skillAttack2)
            ctx.SetAnimatorTrigger(BossSkill2SubStateTrigger);
        // 애니메이션 트리거는 BossManageTable.animation 값을 그대로 사용한다고 가정
        else if (!string.IsNullOrEmpty(_currentBossAttack.animation))
            ctx.SetAnimatorTrigger(_currentBossAttack.animation);
        else
            ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Attack);

        if (HasValidBossParticleEffectKey(_currentBossAttack))
            SpawnBossTableParticleEffect(ctx, _currentBossAttack);
        else
            SpawnAttackEffect(ctx, spawnOnPlayer: true);
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
            if (ctx.FaceTargetWhileAttacking && ctx.PlayerTransform != null)
                RotateTowardsTarget(ctx.EnemyTransform, ctx.PlayerTransform.position, ctx.AttackTurnSpeed);

            if (_attackInProgress && Time.time >= _attackEndTime)
            {
                // 보스 스킬 공격: BossManageTable 기반으로 대미지/속성 적용
                if (!_damageApplied && ctx.PlayerTransform != null)
                {
                    float dist = ctx.GetBoundsDistanceToPlayer();
                    if (dist <= ctx.AttackRange)
                    {
                        var damageable = ctx.PlayerTransform.GetComponent<IDamageable>();
                        if (damageable != null && IsTargetWithinAttackAngle(ctx))
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
                            if (_currentBossAttack != null && _currentBossAttack.attackType != AttackType.normalAttack)
                                PlayBossAreaCastSound(ctx);
                            else
                                PlayAttackHitSound(ctx);
                        }
                    }
                }

                _attackInProgress = false;
                ClearAttackEffect(ctx);
                ctx.RequestState(EnemyStateType.Chase);
            }
        }
        else
        {
            if (ctx.FaceTargetWhileAttacking && ctx.PlayerTransform != null)
                RotateTowardsTarget(ctx.EnemyTransform, ctx.PlayerTransform.position, ctx.AttackTurnSpeed);

            if (!_attackInProgress || Time.time < _attackEndTime)
                return;

            // 공격 타이밍에 플레이어가 사거리 내에 있으면 TakeDamage 호출
            if (!_damageApplied && ctx.PlayerTransform != null)
            {
                float dist = ctx.GetBoundsDistanceToPlayer();
                if (dist <= ctx.AttackRange)
                {
                    var damageable = ctx.PlayerTransform.GetComponent<IDamageable>();
                    if (damageable != null && IsTargetWithinAttackAngle(ctx))
                    {
                        // 이 부분 버프 계산된 공격력 가져오는걸로 수정했습니다.
                        float damage = ctx.AttackPoint;

                        damageable.TakeDamage(damage, DamageType.Physical);
                        _damageApplied = true;
                        PlayAttackHitSound(ctx);
                    }
                }
            }
            _attackInProgress = false;
            ClearAttackEffect(ctx);
            ctx.RequestState(EnemyStateType.Chase);
        }
    }

    public void OnExit(EnemyStateContext ctx)
    {
        if (ctx.Agent != null && ctx.Agent.isActiveAndEnabled && ctx.Agent.isOnNavMesh)
            ctx.Agent.updateRotation = true;

        ClearAttackEffect(ctx);
        _currentBossAttack = null;
    }

    private void ClearAttackEffect(EnemyStateContext ctx)
    {
        if (_currentAttackEffect != null)
        {
            Object.Destroy(_currentAttackEffect);
            _currentAttackEffect = null;
        }
    }

    private static bool HasValidBossParticleEffectKey(BossManageTable atk)
    {
        if (atk == null || string.IsNullOrWhiteSpace(atk.effect))
            return false;
        if (string.Equals(atk.effect, "None", System.StringComparison.OrdinalIgnoreCase))
            return false;
        return atk.effect != "0";
    }

    /// <summary>BossManageTable.effect → Addressable 파티클 키 (PoolableParticleManager)</summary>
    private static void SpawnBossTableParticleEffect(EnemyStateContext ctx, BossManageTable atk)
    {
        if (!HasValidBossParticleEffectKey(atk) || ctx.EnemyTransform == null)
            return;
        var pm = PoolableParticleManager.Instance;
        if (pm == null)
            return;

        pm.Preload(atk.effect);
        bool onPlayer = atk.attackType != AttackType.normalAttack;
        Transform follow = onPlayer && ctx.PlayerTransform != null ? ctx.PlayerTransform : ctx.EnemyTransform;
        pm.SpawnParticle(new ParticleSpawnContext(atk.effect, follow, follow: true));
    }

    private void SpawnAttackEffect(EnemyStateContext ctx, bool spawnOnPlayer)
    {
        if (ctx.AttackEffectPrefab == null) return;

        if (_currentAttackEffect != null)
            Object.Destroy(_currentAttackEffect);

        Transform anchor = ctx.EnemyTransform;
        Transform parent = ctx.EnemyTransform;

        if (spawnOnPlayer && ctx.PlayerTransform != null)
        {
            anchor = ctx.PlayerTransform;
            parent = ctx.PlayerTransform;
        }

        _currentAttackEffect = Object.Instantiate(
            ctx.AttackEffectPrefab,
            anchor.position + Vector3.up * 1f,
            Quaternion.identity,
            parent
        );

        float scaleMultiplier = Mathf.Max(0.1f, ctx.AttackEffectScaleMultiplier);
        _currentAttackEffect.transform.localScale *= scaleMultiplier;
    }

    private static void SpawnSkillAttackEffectAtPlayerHead(EnemyStateContext ctx)
    {
        if (ctx.SkillAttackEffectPrefab == null || ctx.PlayerTransform == null)
            return;

        Vector3 spawnPos = ctx.PlayerTransform.position + ctx.SkillAttackEffectOffset;
        GameObject effect = Object.Instantiate(ctx.SkillAttackEffectPrefab, spawnPos, Quaternion.identity);
        if (SkillAttackEffectLifeTime > 0f)
            Object.Destroy(effect, SkillAttackEffectLifeTime);
    }

    private static void PlayAttackHitSound(EnemyStateContext ctx)
    {
        if (ctx.AttackSoundId <= 0 || SoundManager.Instance == null || ctx.EnemyTransform == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(ctx.AttackSoundId, ctx.EnemyTransform.position);
    }

    private static void PlayBossAreaPrepareSound(EnemyStateContext ctx)
    {
        if (ctx.BossAreaAttackPrepareSoundId <= 0 || SoundManager.Instance == null || ctx.EnemyTransform == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(ctx.BossAreaAttackPrepareSoundId, ctx.EnemyTransform.position);
    }

    private static void PlayBossAreaCastSound(EnemyStateContext ctx)
    {
        if (ctx.BossAreaAttackCastSoundId <= 0 || SoundManager.Instance == null || ctx.EnemyTransform == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(ctx.BossAreaAttackCastSoundId, ctx.EnemyTransform.position);
    }

    private static bool IsTargetWithinAttackAngle(EnemyStateContext ctx)
    {
        if (ctx?.EnemyTransform == null || ctx.PlayerTransform == null)
            return false;

        Vector3 toPlayer = ctx.PlayerTransform.position - ctx.EnemyTransform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f)
            return true;

        float angle = Vector3.Angle(ctx.EnemyTransform.forward, toPlayer.normalized);
        return angle <= Mathf.Clamp(ctx.MaxAttackAngle, 1f, 180f);
    }

    private static void RotateTowardsTarget(Transform self, Vector3 targetPos, float turnSpeed)
    {
        if (self == null || turnSpeed <= 0f)
            return;

        Vector3 toTarget = targetPos - self.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(toTarget.normalized);
        float t = Mathf.Clamp01(turnSpeed * Time.deltaTime);
        self.rotation = Quaternion.Slerp(self.rotation, targetRot, t);
    }
}