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
    [SerializeField][Tooltip("비워두면 Resources 또는 전역 DB 사용.")]
    private MonsterAssetDatabase assetDatabaseOverride;

    private EnemyStateContext _ctx;
    private Dictionary<EnemyStateType, IEnemyState> _states;
    private IEnemyState _current;
    private EnemyStateType _currentType;

    public EnemyStateContext Context => _ctx;
    public EnemyStateType CurrentStateType => _currentType;
    public bool IsAlive => _currentType != EnemyStateType.Dead;
    public bool isMoving => Context.Agent.velocity.sqrMagnitude > 0.1f;

    private void Awake()
    {
        var agent = GetComponent<NavMeshAgent>();
        var statPresenter = GetComponent<EnemyStatPresenter>();
        var skillHandler = GetComponent<EnemySkillHandler>();
        var effectController = GetComponent<EffectController>();

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
            SkillHandler = skillHandler,
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
            { EnemyStateType.Dead, new EnemyStateDead() }
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
                if (attackEffectPrefab == null && entry.attackEffectPrefab != null)
                    attackEffectPrefab = entry.attackEffectPrefab;
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

            if (_ctx.SkillHandler != null && _ctx.PlayerTransform != null)
            {
                _ctx.SkillHandler.SetPlayerTransform(_ctx.PlayerTransform);
                _ctx.SkillHandler.Init();
            }
        }

        EnemyStatData data = _ctx.StatPresenter?.Data;
        if (data != null && _ctx.Agent != null)
        {
            _ctx.Agent.speed = data.monsterSpeed;
            _ctx.Agent.stoppingDistance = data.attackRange;
        }

        // 스폰되면 바로 chase 상태로 전환
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

        _current.OnUpdate(_ctx);
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
            ChangeState(EnemyStateType.Dead);
            return;
        }

        if (_currentType == EnemyStateType.Attack)
            ChangeState(EnemyStateType.Onhit);
        else if (_currentType != EnemyStateType.Onhit && _currentType != EnemyStateType.Dead)
            ChangeState(EnemyStateType.Onhit);
    }

    /// <summary>
    /// 외부에서 넉백 또는 당기기시 호출
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="distance"></param>
    /// <param name="duration"></param>
    public void ApplyKnockback(Vector3 direction, float distance, float duration)
    {
        if (!IsAlive) return;

        _ctx.PendingKnockback = new KnockbackInfo
        {
            direction = direction,
            distance = distance,
            duration = duration
        };

        if (_currentType != EnemyStateType.Onhit && _currentType != EnemyStateType.Dead)
        {
            ChangeState(EnemyStateType.Onhit);
        }
    }

    /// <summary>
    /// 풀에서 재스폰될 때 호출. 체력·상태·에이전트 리셋.
    /// </summary>
    public void OnSpawnFromPool()
    {
        _ctx.SpawnPosition = transform.position;
        _ctx.Initialize();
        // 풀 재사용 시 플레이어 참조 갱신 (파괴/리스폰 시 stale 참조 방지)
        RefreshPlayerTransform();
        if (_ctx.SkillHandler != null && _ctx.PlayerTransform != null)
            _ctx.SkillHandler.SetPlayerTransform(_ctx.PlayerTransform);
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
        ChangeState(EnemyStateType.Chase);
    }
}
