using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 몬스터 상태 머신.
/// 상태 정의(EnemyStateType, enum)는 별도 파일에 정의.
/// 각 상태 별 동작(EnemyState****)은 각 파일에서 참조해 사용.
/// 스폰 시 Chase로 시작, 공격 사거리 내면 Attack, 피격 시 Onhit, 사망 시 Dead 등으로 전환.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(EnemyStatPresenter))]
[RequireComponent(typeof(EffectController))]
public class EnemyStateMachine : MonoBehaviour, IPoolableRespawnable, IDamageable,IKnockbackable
{
    [Header("보스 피격 모션 제어")]
    [SerializeField, Tooltip("활성화 시 보스는 Attack/Spawn 상태에서 피격(Onhit)으로 끊기지 않습니다.")]
    private bool suppressBossOnhitDuringAttack = false;
    [SerializeField, Tooltip("보스 Onhit 재생 최소 간격(초). 너무 자주 피격 모션이 반복되는 것을 방지합니다.")]
    private float bossOnhitCooldownSeconds = 0.8f;

    [Header("플레이어 참조 설정")]
    [SerializeField][Tooltip("추적할 플레이어. 비워두면 'Player' 태그를 가진 오브젝트를 자동 검색합니다.")]
    private Transform playerTransformOverride;

    [Header("에셋 (비워두면 몬스터 ID로 MonsterAssetDatabase에서 자동 조회)")]
    [SerializeField][Tooltip("비워두면 본인/자식에서 Animator 자동 검색.")]
    private Animator animator;

    [SerializeField][Tooltip("비워두면 DB에서 monsterId로 조회.")]
    private MonsterAnimationConfig animationConfig;

    [SerializeField][Tooltip("비워두면 DB에서 monsterId로 조회.")]
    private GameObject attackEffectPrefab;
    [SerializeField][Tooltip("스킬 공격형 일반 몬스터 공격 시 출력할 연출 VFX. 비워두면 DB에서 monsterId로 조회.")]
    private GameObject skillAttackEffectPrefab;
    [SerializeField][Tooltip("스킬 공격형 이펙트 위치 오프셋(플레이어 기준). DB 값 우선")]
    private Vector3 skillAttackEffectOffset;

    [SerializeField, Min(0.1f)]
    [Tooltip("몬스터 공격 이펙트 크기 배율. 1=기본, 0.8=20% 축소")]
    private float attackEffectScaleMultiplier = 0.5f;

    [Header("타겟 바라보기")]
    [SerializeField, Min(0f)]
    [Tooltip("추적 중 플레이어를 바라보는 회전 속도")]
    private float chaseTurnSpeed = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("공격 중 플레이어를 바라보는 회전 속도")]
    private float attackTurnSpeed = 10f;

    [SerializeField, Tooltip("공격 상태에서 플레이어를 바라보도록 회전을 보정합니다.")]
    private bool faceTargetWhileAttacking = true;

    [SerializeField, Range(1f, 180f)]
    [Tooltip("타격 판정 시 허용되는 정면 각도(도)")]
    private float maxAttackAngle = 60f;
    
    [SerializeField][Tooltip("비워두면 DB에서 monsterId로 조회.")]
    private GameObject onHitEffectPrefab;
    
    [Header("SFX 오버라이드 (SoundTable ID, 0이면 MonsterAssetDatabase 값 사용)")]
    [SerializeField] private int attackSoundId;
    [SerializeField] private int onHitSoundId;
    [SerializeField] private int dieSoundId;
    [SerializeField] private int footstepSoundId;
    [SerializeField] private int skillPrepareSoundId;
    [SerializeField] private int skillCastSoundId;
    [SerializeField] private int bossSpawnSoundId;
    [SerializeField] private int bossAreaAttackPrepareSoundId;
    [SerializeField] private int bossAreaAttackCastSoundId;

    [SerializeField][Tooltip("비워두면 Resources 또는 전역 DB 사용.")]
    private MonsterAssetDatabase assetDatabaseOverride;

    private EnemyStateContext _ctx;
    private Dictionary<EnemyStateType, IEnemyState> _states;
    private IEnemyState _current;
    private EnemyStateType _currentType;
    private float _nextBossOnhitAllowedTime;

    public EnemyStateContext Context => _ctx;
    public EnemyStateType CurrentStateType => _currentType;
    public bool IsAlive => _currentType != EnemyStateType.Dead;
    public bool isMoving => Context.Agent.velocity.sqrMagnitude > 0.1f;

    //넉백 필드
    private bool _isKnockbackActive;
    private KnockbackInfo _knockbackInfo;
    private float _knockbackSpeed;
    private float _knockbackElapsedTime;
    private Vector3 _knockbackStartPos;
    private bool _wasRootMotionEnabled;

    private bool _wasKinematic;
    private Rigidbody _rb;
    private void Awake()
    {
        var agent = GetComponent<NavMeshAgent>();
        var statPresenter = GetComponent<EnemyStatPresenter>();
        var effectController = GetComponent<EffectController>();
        if (_rb == null)
            _rb = GetComponent<Rigidbody>();
        ResolveAssets(statPresenter);

        _ctx = new EnemyStateContext
        {
            EnemyTransform = transform,
            SpawnPosition = transform.position,
            PlayerTransform = null,
            Agent = agent,
            StatPresenter = statPresenter,
            Animator = animator,
            AnimationConfig = animationConfig,
            IsBoss = statPresenter != null && statPresenter.IsBoss,
            AttackEffectPrefab = attackEffectPrefab,
            SkillAttackEffectPrefab = skillAttackEffectPrefab,
            SkillAttackEffectOffset = skillAttackEffectOffset,
            AttackEffectScaleMultiplier = attackEffectScaleMultiplier,
            ChaseTurnSpeed = chaseTurnSpeed,
            AttackTurnSpeed = attackTurnSpeed,
            FaceTargetWhileAttacking = faceTargetWhileAttacking,
            MaxAttackAngle = maxAttackAngle,
            OnHitEffectPrefab = onHitEffectPrefab,
            AttackSoundId = attackSoundId,
            OnHitSoundId = onHitSoundId,
            DieSoundId = dieSoundId,
            FootstepSoundId = footstepSoundId,
            SkillPrepareSoundId = skillPrepareSoundId,
            SkillCastSoundId = skillCastSoundId,
            BossSpawnSoundId = bossSpawnSoundId,
            BossAreaAttackPrepareSoundId = bossAreaAttackPrepareSoundId,
            BossAreaAttackCastSoundId = bossAreaAttackCastSoundId,
            EnemyEffectController = effectController
        };
        _ctx.Initialize();
        _ctx.SetStateChangeCallback(OnRequestStateChange);

        _states = new Dictionary<EnemyStateType, IEnemyState>
        {
            { EnemyStateType.Idle, new EnemyStateIdle() },
            { EnemyStateType.Chase, new EnemyStateChase() },
            { EnemyStateType.Attack, new EnemyStateAttack() },
            { EnemyStateType.Onhit, new EnemyStateOnhit() },
            { EnemyStateType.Dead, new EnemyStateDead() },
            { EnemyStateType.Spawn, new EnemyStateSpawn() }
        };
    }

    /// <summary>
    /// 프리팹에 넣지 않은 에셋을 DB 또는 자동 검색으로 채움. Animator, AnimationConfig, AttackEffectPrefab.
    /// </summary>
    private void ResolveAssets(EnemyStatPresenter statPresenter)
    {
        if (animator == null)
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

        var db = assetDatabaseOverride != null ? assetDatabaseOverride : MonsterAssetDatabase.Instance;
        if (db != null && statPresenter != null && statPresenter.monsterIdFromDataManager != 0)
        {
            var entry = db.GetEntry(statPresenter.monsterIdFromDataManager);
            if (entry != null)
            {
                if (animationConfig == null && entry.animationConfig != null)
                    animationConfig = entry.animationConfig;

                // 몬스터별 OverrideController 자동 적용 (공통 Controller + 클립 교체 방식)
                if (animator != null && entry.animatorOverrideController != null)
                    animator.runtimeAnimatorController = entry.animatorOverrideController;

                if (attackEffectPrefab == null && entry.attackEffectPrefab != null)
                    attackEffectPrefab = entry.attackEffectPrefab;
                if (skillAttackEffectPrefab == null && entry.skillAttackEffectPrefab != null)
                    skillAttackEffectPrefab = entry.skillAttackEffectPrefab;
                skillAttackEffectOffset = entry.skillAttackEffectOffset;

                if (onHitEffectPrefab == null && entry.onHitEffectPrefab != null)
                    onHitEffectPrefab = entry.onHitEffectPrefab;

                if (attackSoundId == 0 && entry.attackSoundId != 0)
                    attackSoundId = entry.attackSoundId;
                if (onHitSoundId == 0 && entry.onHitSoundId != 0)
                    onHitSoundId = entry.onHitSoundId;
                if (dieSoundId == 0 && entry.dieSoundId != 0)
                    dieSoundId = entry.dieSoundId;
                if (footstepSoundId == 0 && entry.footstepSoundId != 0)
                    footstepSoundId = entry.footstepSoundId;
                if (skillPrepareSoundId == 0 && entry.skillPrepareSoundId != 0)
                    skillPrepareSoundId = entry.skillPrepareSoundId;
                if (skillCastSoundId == 0 && entry.skillCastSoundId != 0)
                    skillCastSoundId = entry.skillCastSoundId;
                if (bossSpawnSoundId == 0 && entry.bossSpawnSoundId != 0)
                    bossSpawnSoundId = entry.bossSpawnSoundId;
                if (bossAreaAttackPrepareSoundId == 0 && entry.bossAreaAttackPrepareSoundId != 0)
                    bossAreaAttackPrepareSoundId = entry.bossAreaAttackPrepareSoundId;
                if (bossAreaAttackCastSoundId == 0 && entry.bossAreaAttackCastSoundId != 0)
                    bossAreaAttackCastSoundId = entry.bossAreaAttackCastSoundId;
            }
        }
    }

    private void Start()
    {
        RefreshPlayerTransform();

        // 일반 몬스터는 모두 기본 근접/원거리 로직을 사용하고,
        // 보스 몬스터만 BossManageTable 기반 스킬 패턴을 사용하도록 설정.
        if (_ctx.IsBoss)
        {
            if (DataManager.Instance?.BossManageDict != null && DataManager.Instance.BossManageDict.Count > 0)
            {
                _ctx.BossAttackManager = new BossAttackManager(DataManager.Instance.BossManageDict.Values);
            }

        }

        EnemyStatData data = _ctx.StatPresenter?.Data;
        if (data != null && _ctx.Agent != null)
        {
            _ctx.Agent.speed = data.monsterSpeed;
            _ctx.Agent.stoppingDistance = data.attackRange;
        }

        // 보스: 스폰 연출 후 Chase. 일반몹: 바로 Chase
        if (_ctx.IsBoss)
            ChangeState(EnemyStateType.Spawn);
        else
            ChangeState(EnemyStateType.Chase);
    }

    /// <summary>
    /// 플레이어 Transform 참조 갱신. Start/OnSpawnFromPool에서 호출.
    /// </summary>
    private void RefreshPlayerTransform()
    {
        if (playerTransformOverride != null && playerTransformOverride)
        {
            _ctx.PlayerTransform = playerTransformOverride;
        }
        else
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            _ctx.PlayerTransform = go != null ? go.transform : null;
        }
    }

    private void Update()
    {
        if (_current == null) return;

        // 보스 공격 패턴 쿨타임/가중치 갱신
        if (_ctx.IsBoss && _ctx.BossAttackManager != null)
        {
            _ctx.BossAttackManager.Tick(Time.deltaTime);
        }
        UpdateKnockback();

        _current.OnUpdate(_ctx);
    }
    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        if (!IsAlive) return;

        _knockbackInfo = new KnockbackInfo
        {
            direction = direction,
            distance = distance,
            duration = duration
        };

        _knockbackSpeed = distance / duration;
        _knockbackElapsedTime = 0f;
        _isKnockbackActive = true;
        _knockbackStartPos = _ctx.EnemyTransform.position;

        if (_ctx.Agent != null && _ctx.Agent.isActiveAndEnabled)
        {
            _ctx.Agent.enabled = false;
        }
        if (_ctx.Animator != null)
        {
            _wasRootMotionEnabled = _ctx.Animator.applyRootMotion;
            _ctx.Animator.applyRootMotion = false;
        }
        if (_rb != null)
        {
            _wasKinematic = _rb.isKinematic;
            _rb.isKinematic = true;
        }
    }

    private void UpdateKnockback()
    {
        if (!_isKnockbackActive) return;

        if (_knockbackElapsedTime < _knockbackInfo.duration)
        {
            _knockbackElapsedTime += Time.deltaTime;

            Vector3 direction = _knockbackInfo.direction;
            direction.y = 0f;
            direction = direction.normalized;

            float step = _knockbackSpeed * Time.deltaTime;
            _ctx.EnemyTransform.position += direction * step;

            float movedDist = Vector3.Distance(_knockbackStartPos, _ctx.EnemyTransform.position);
        }
        else
        {
            _isKnockbackActive = false;

            float finalDist = Vector3.Distance(_knockbackStartPos, _ctx.EnemyTransform.position);
            if (_ctx.Animator != null)
            {
                _ctx.Animator.applyRootMotion = _wasRootMotionEnabled;
            }
            if (_rb != null)
            {
                _rb.isKinematic = _wasKinematic;
            }
            if (_ctx.Agent != null)
            {
                _ctx.Agent.enabled = true;

                if (NavMesh.SamplePosition(_ctx.EnemyTransform.position, out var hit, 2.0f, NavMesh.AllAreas))
                {
                    _ctx.Agent.Warp(hit.position);
                }
                else
                {
                    _ctx.Agent.Warp(_ctx.EnemyTransform.position);
                }
            }
        }
    }
    private void OnRequestStateChange(EnemyStateType next)
    {
        ChangeState(next);
    }

    private void ChangeState(EnemyStateType next)
    {
        //Debug.Log($"[EnemyStateMachine] {Context.Agent.name} RequestState: {next}");
        
        if (_current != null)
            _current.OnExit(_ctx);

        _currentType = next;
        _current = _states.TryGetValue(next, out var state) ? state : null;

        if (_current != null)
            _current.OnEnter(_ctx);
    }

    /// <summary>
    /// 외부(플레이어 공격 등)에서 피격 시 호출. 데미지 적용 후 Onhit 상태로 전환
    /// </summary>
    public void TakeDamage(float damage, DamageType type = DamageType.Physical)
    {
        if (!IsAlive) return;
        _ctx.TakeDamage(damage,type);

        if (_ctx.CurrentHealth <= 0f)
        {
            _ctx.EnemyEffectController.OnDeath();
            ChangeState(EnemyStateType.Dead);
            return;
        }

        if (_ctx.IsBoss)
        {
            if (suppressBossOnhitDuringAttack &&
                (_currentType == EnemyStateType.Attack || _currentType == EnemyStateType.Spawn))
            {
                return;
            }

            if (Time.time < _nextBossOnhitAllowedTime)
                return;

            _nextBossOnhitAllowedTime = Time.time + Mathf.Max(0f, bossOnhitCooldownSeconds);
        }

        if (_currentType == EnemyStateType.Attack)
            ChangeState(EnemyStateType.Onhit);
        else if (_currentType != EnemyStateType.Onhit && _currentType != EnemyStateType.Dead)
            ChangeState(EnemyStateType.Onhit);
    }


    /// <summary>
    /// 풀에서 재스폰될 때 호출. 체력·상태·에이전트 리셋.
    /// </summary>
    public void OnSpawnFromPool()
    {
        _ctx.SpawnPosition = transform.position;
        _ctx.Initialize();
        RefreshPlayerTransform();
        if (_ctx.Agent != null && _ctx.Agent.isOnNavMesh)
        {
            _ctx.Agent.isStopped = false;
            _ctx.Agent.ResetPath();
        }
        EnemyStatData data = _ctx.StatPresenter?.Data;
        if (data != null && _ctx.Agent != null)
        {
            _ctx.Agent.speed = data.monsterSpeed;
            _ctx.Agent.stoppingDistance = data.attackRange;
        }

        // 애니메이터 초기화 + Idle로 보내기
        if (_ctx.Animator != null)
        {
            _ctx.Animator.Rebind();
            _ctx.Animator.Update(0f);
            _ctx.SetAnimatorTrigger(MonsterAnimationConfig.TriggerKey.Idle);
        }

        // 보스는 풀 재스폰에서도 Spawn 연출 후 Chase로 가야 일관적임
        if (_ctx.IsBoss)
            ChangeState(EnemyStateType.Spawn);
        else
            ChangeState(EnemyStateType.Chase);
    }

    /// <summary>
    /// 애니메이션 이벤트에서 호출: 발걸음 등 (SoundManager / Combat SFX).
    /// </summary>
    public void Anim_PlayFootstep()
    {
        if (_ctx == null || _ctx.FootstepSoundId <= 0 || SoundManager.Instance == null)
            return;
        SoundManager.Instance.PlayCombatSfxAt(_ctx.FootstepSoundId, transform.position);
    }
}
