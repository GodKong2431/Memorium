using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 상태 머신과 각 상태가 공유하는 컨텍스트.
/// 플레이어/에이전트(적)/스탯/현재 체력/상태 전환 요청 등.
/// </summary>
public class EnemyStateContext
{
    /// <summary>몬스터별 런타임 상태(IEnemyState는 공유 인스턴스).</summary>
    public EnemyInstanceState Instance { get; } = new EnemyInstanceState();

    public Transform EnemyTransform { get; set; }
    public Transform PlayerTransform { get; set; }
    public NavMeshAgent Agent { get; set; }
    public EnemyStatPresenter StatPresenter { get; set; }

    public EffectController EnemyEffectController { get; set; }
    public Animator Animator { get; set; }

    /// <summary>
    /// 애니메이션 트리거/파라미터 이름 매핑. null이면 기본값(enum 이름) 사용.
    /// </summary>
    public MonsterAnimationConfig AnimationConfig { get; set; }

    /// <summary>
    /// 최초 스폰 위치 (리스폰 시 사용)
    /// </summary>
    public Vector3 SpawnPosition { get; set; }

    /// <summary>
    /// 현재 체력 (런타임). 피격 시 감소, 0 이하면 Dead 전환
    /// </summary>
    public float CurrentHealth { get; private set; }

    public float MaxHealth => StatPresenter?.Data?.monsterHealth ?? 100f;
    public float AttackRange => StatPresenter?.Data?.attackRange ?? 1.5f;

    // 원본값
    public float BaseAttackPoint => StatPresenter?.Data?.monsterAttackpoint ?? 10f;
    public float BaseAttackSpeed => StatPresenter?.Data?.monsterAttackspeed ?? 1f;
    public float BaseMoveSpeed => StatPresenter?.Data?.monsterSpeed ?? 3f;
   public float BaseDefense => StatPresenter?.Data?.monsterDefense ?? 0f; // 방어력 추가 시
    public KnockbackInfo? PendingKnockback { get; set; }
    // 디버프 반영 최종값

    /// <summary>
    /// 버프 / 디버프 적용된 공격력.
    /// </summary>
    public float AttackPoint => GetModifiedStat(StatType.ATK, BaseAttackPoint);

    /// <summary>
    /// 버프 / 디버프 적용된 공격 속도.
    /// </summary>
    public float AttackSpeed => GetModifiedStat(StatType.ATK_SPEED, BaseAttackSpeed);

    /// <summary>
    /// 버프/디버프 적용된 이동 속도.
    /// </summary>
    public float MoveSpeed => GetModifiedStat(StatType.MOVE_SPEED, BaseMoveSpeed);

    /// <summary>
    /// 버프/디버프 적용된 방어력
    /// </summary>
    public float Defense => GetModifiedStat(StatType.PHYS_DEF, BaseDefense);

    /// <summary>StatPresenter.IsBoss 또는 인스펙터 isBoss 중 하나라도 true면 보스. (인스펙터 명시적 설정 우선)</summary>
    private bool _isBossFallback;
    public bool IsBoss { get => (StatPresenter?.IsBoss ?? false) || _isBossFallback; set => _isBossFallback = value; }

    /// <summary>공격 타격 VFX 프리팹 — 스폰 시 플레이어에 부착.</summary>
    public GameObject AttackEffectPrefab { get; set; }
    /// <summary>스킬 준비 이펙트 프리팹.</summary>
    public GameObject SkillPrepareEffectPrefab { get; set; }
    public SkillEffectAttachTarget SkillPrepareAttachTo { get; set; }
    /// <summary>스킬 시전 이펙트 프리팹.</summary>
    public GameObject SkillCastEffectPrefab { get; set; }
    public SkillEffectAttachTarget SkillCastAttachTo { get; set; }
    /// <summary>보스 일반 공격 전용 프리팹. 비면 <see cref="AttackEffectPrefab"/> 사용.</summary>
    public GameObject BossNormalAttackEffectPrefab { get; set; }
    /// <summary>적 로컬 기준 추가 오프셋. DB skillAttackEffectOffset.</summary>
    public Vector3 SkillAttackEffectOffset { get; set; }
    /// <summary>스킬 준비 프리팹의 부모 기준 로컬 위치·회전·배율.</summary>
    public SkillEffectSpawnTransform SkillPrepareSpawn { get; set; }
    /// <summary>스킬 시전 프리팹의 부모 기준 로컬 위치·회전·배율.</summary>
    public SkillEffectSpawnTransform SkillCastSpawn { get; set; }
    public float AttackEffectScaleMultiplier { get; set; } = 1f;
    public float ChaseTurnSpeed { get; set; } = 6f;
    public float AttackTurnSpeed { get; set; } = 10f;
    public bool FaceTargetWhileAttacking { get; set; } = true;
    public float MaxAttackAngle { get; set; } = 60f;

    /// <summary>피격 VFX 프리팹 — EnemyStateOnhit에서 몬스터에 부착.</summary>
    public GameObject OnHitEffectPrefab { get; set; }

    /// <summary>SoundTable·SoundManager 전투 SFX ID (0 = 없음)</summary>
    public int AttackSoundId { get; set; }
    public int OnHitSoundId { get; set; }
    public int DieSoundId { get; set; }
    public int FootstepSoundId { get; set; }

    /// <summary>스킬 시전 준비(SkillCaster TryCastSkill 등). 0 = 없음</summary>
    public int SkillPrepareSoundId { get; set; }
    /// <summary>스킬 발동(M3 실행 직전). 0 = 없음</summary>
    public int SkillCastSoundId { get; set; }
    /// <summary>보스 스폰 연출(Spawn 상태). 0 = 없음</summary>
    public int BossSpawnSoundId { get; set; }
    /// <summary>보스 스킬 시전 준비. 0 = 없음</summary>
    public int BossAreaAttackPrepareSoundId { get; set; }
    /// <summary>보스 범위 공격 히트 타이밍. 0 = 없음</summary>
    public int BossAreaAttackCastSoundId { get; set; }

    /// <summary>보스 공격 패턴 관리(일반/스킬).</summary>
    public BossAttackManager BossAttackManager { get; set; }

    /// <summary>
    /// 보스 스킬 공격을 사용할지 여부.
    /// 일반 몬스터는 항상 false로 처리하여 "스킬 공격형 몬스터"도 로직상 일반 원거리 몬스터와 동일하게 동작하게 한다.
    /// </summary>
    public bool IsSkillAttackType => IsBoss && BossAttackManager != null;

    private Action<EnemyStateType> _requestStateChange;

    public void Initialize(float? startHealth = null)
    {
        CurrentHealth = startHealth ?? MaxHealth;
    }

    /// <summary>
    /// <see cref="BossManageTable.castingDelay"/> / <see cref="BossManageTable.castingTime"/>(초). CSV 값 그대로(MonsterAssetDatabase 배율·오프셋 없음).
    /// </summary>
    public void ComputeBossManagedCastingDurations(BossManageTable row, out float castingDelay, out float castingTime)
    {
        castingDelay = 0f;
        castingTime = 0f;
        if (row == null) return;
        castingDelay = Mathf.Max(0f, row.castingDelay);
        castingTime = Mathf.Max(0f, row.castingTime);
    }

    public void SetStateChangeCallback(Action<EnemyStateType> callback)
    {
        _requestStateChange = callback;
    }

    public void RequestState(EnemyStateType next)
    {
        _requestStateChange?.Invoke(next);
    }

    /// <summary>
    /// 피격 시 호출. 데미지 적용 후 Onhit 상태로 전환할지 등은 StateMachine에서 처리
    /// </summary>
    public void TakeDamage(float damage, DamageType type = DamageType.Physical)
    {
        //Debug.Log($"[EnemyStateContext] TakeDamage: {damage}");

        if (type == DamageType.FixedPercentageDamage)
        {
            CurrentHealth -= CurrentHealth * (damage * 0.01f);
            return;
        }
        damage = Mathf.Max(0f, damage - Defense);
        CurrentHealth -= (damage);
    }
    /// <summary>
    /// 플레이어가 존재하고 살아있는지. (PlayerHealth 등 체력 컴포넌트가 있으면 해당 로직으로 확장 가능)
    /// </summary>
    public bool IsPlayerAlive()
    {
        // 현재는 플레이어 존재 여부만 체크
        if (PlayerTransform == null) return false;
        return true;
    }

    /// <summary>
    /// 설정(AnimationConfig)에 따라 트리거 이름을 조회한 뒤 Animator에 설정.
    /// </summary>
    public void SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey key)
    {
        string trigger = AnimationConfig != null
            ? AnimationConfig.GetTrigger(key)
            : MonsterAnimationConfig.GetDefaultTrigger(key);
        if (Animator != null && !string.IsNullOrEmpty(trigger))
            Animator.SetTrigger(trigger);
    }

    /// <summary>
    /// 커스텀 트리거명 직접 지정 시 사용 (예: BossManageTable.animation 등).
    /// </summary>
    public void SetAnimatorTrigger(string trigger)
    {
        if (Animator != null && !string.IsNullOrEmpty(trigger))
            Animator.SetTrigger(trigger);
    }

    public void SetAnimatorFloat(string paramName, float value)
    {
        if (Animator == null || string.IsNullOrEmpty(paramName)) return;
        Animator.SetFloat(paramName, value);
    }

    public void SetLocomotion(float value)
    {
        if (AnimationConfig == null)
        {
            // 기본값은 "Locomotion"으로 가정
            SetAnimatorFloat("Locomotion", value);
            return;
        }
        SetAnimatorFloat(AnimationConfig.LocomotionParam, value);
    }

    public float GetModifiedStat(StatType type, float baseValue)
    {
        if (EnemyEffectController == null) return baseValue;
        return EnemyEffectController.GetModifiedStat(type, baseValue);
    }

    private Collider _enemyCollider;
    private Collider _playerCollider;

    public float GetBoundsDistanceToPlayer()
    {
        if (EnemyTransform == null || PlayerTransform == null)
            return float.PositiveInfinity;

        if (_enemyCollider == null)
            _enemyCollider = EnemyTransform.GetComponentInChildren<Collider>();
        if (_playerCollider == null)
            _playerCollider = PlayerTransform.GetComponentInChildren<Collider>();

        if (_enemyCollider == null || _playerCollider == null)
            return Vector3.Distance(EnemyTransform.position, PlayerTransform.position);

        // 각 콜라이더에서 서로를 향한 가장 가까운 점
        Vector3 enemyPoint  = _enemyCollider.ClosestPoint(PlayerTransform.position);
        Vector3 playerPoint = _playerCollider.ClosestPoint(EnemyTransform.position);

        return Vector3.Distance(enemyPoint, playerPoint);
    }
}
