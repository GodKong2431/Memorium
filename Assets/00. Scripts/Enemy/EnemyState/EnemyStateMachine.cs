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
public class EnemyStateMachine : MonoBehaviour, IPoolableRespawnable, IDamageable
{
    [Header("플레이어 참조 설정")]
    [SerializeField][Tooltip("추적할 플레이어. 비워두면 'Player' 태그를 가진 오브젝트를 자동 검색합니다.")]
    private Transform playerTransformOverride;

    [SerializeField][Tooltip("현재 적 개체가 사용할 애니메이터입니다.")]
    private Animator animator; // 애니메이터는 현재 미구현, 향후 추가를 위해 직렬화만 해둠
    [SerializeField][Tooltip("현재 적 개체의 보스 몬스터 여부입니다.")]
    private bool isBoss;
    [SerializeField][Tooltip("공격 시 나타나는 이펙트 프리팹입니다.")]
    private GameObject attackEffectPrefab; // 공격 이펙트 추가 예정 (인스펙터 할당)
    // [SerializeField] AudioClip attackSound; // 공격 효과음 추가 예정
    // [SerializeField] AudioClip hitSound;    // 피격 효과음 추가 예정
    // [SerializeField] AudioClip deathSound;  // 사망 효과음 추가 예정

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
        // 스킬 공격형 몬스터는 EnemySkillHandler+SkillCaster가 있는 프리팹 사용 필요 (예: EarthWizardEnemy)
        if (statPresenter != null && statPresenter.monsterIdFromDataManager != 0 && skillHandler == null)
        {
            if (MonsterDataProvider.IsSkillAttackMonster(statPresenter.monsterIdFromDataManager))
                Debug.LogWarning($"[EnemyStateMachine] 스킬 공격형 몬스터(ID:{statPresenter.monsterIdFromDataManager})에 EnemySkillHandler가 없습니다. EnemyListManager에서 해당 프리팹을 스킬용 프리팹으로 교체하세요.");
        }

        var agent = GetComponent<NavMeshAgent>();
        var effectController = GetComponent<EffectController>();

        _ctx = new EnemyStateContext
        {
            EnemyTransform = transform,
            SpawnPosition = transform.position,
            PlayerTransform = null,
            Agent = agent,
            StatPresenter = statPresenter,
            Animator = animator,
            IsBoss = isBoss,
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

    private void Start()
    {
        // 1순위: 인스펙터에서 직접 지정한 플레이어 Transform
        if (playerTransformOverride != null)
        {
            _ctx.PlayerTransform = playerTransformOverride;
        }
        else
        {
            // 2순위: 'Player' 태그를 가진 오브젝트 자동 검색 (테스트하다가 좀 missing 당해서 임의로 넣었습니다~ 이건 나중에 확인하고 통합 예정입니다~~~)
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                _ctx.PlayerTransform = go.transform;
            }
            else
            {
                Debug.LogWarning("[EnemyStateMachine] 'Player' 태그 오브젝트를 찾을 수 없습니다.");
            }
        }

        if (_ctx.SkillHandler != null)
        {
            _ctx.SkillHandler.SetPlayerTransform(_ctx.PlayerTransform);
            int skillId = _ctx.StatPresenter?.SkillId ?? 0;
            _ctx.SkillHandler.Init(skillId > 0 ? skillId : 4000001);
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

    private void Update()
    {
        if (_current == null) return;
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
    public void TakeDamage(float damage, DamageType type= DamageType.Physical)
    {
        if (!IsAlive) return;
        _ctx.TakeDamage(damage);

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
    /// 풀에서 재스폰될 때 호출. 체력·상태·에이전트 리셋.
    /// </summary>
    public void OnSpawnFromPool()
    {
        _ctx.SpawnPosition = transform.position;
        _ctx.Initialize();
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
