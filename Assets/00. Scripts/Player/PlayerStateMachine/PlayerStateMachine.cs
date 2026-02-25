using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerStatPresenter))]
public class PlayerStateMachine : MonoBehaviour, IDamageable
{
    [SerializeField]
    [Tooltip("현재 적 개체가 사용할 애니메이터입니다.")]
    private Animator animator; // 애니메이터는 현재 미구현, 향후 추가를 위해 직렬화만 해둠

    [SerializeField]
    [Tooltip("공격 시 나타나는 이펙트 프리팹입니다.")]
    private GameObject attackEffectPrefab;

    [SerializeField] private float angularTime;
    [SerializeField] private float stopAngle;

    public PlayerStateContext _ctx { get; private set; }
    private Dictionary<PlayerStateType, IPlayerState> _states;
    //private IPlayerState _current;
    //private PlayerStateType _currentType;

    public PlayerStateType CurrentType;

    private StateMachine<PlayerStateContext, IPlayerState, PlayerStateType> playerStateMachine;

    bool IsComplete = false;

    private void init()
    {
        var agent = GetComponent<NavMeshAgent>();
        var statPresenter = GetComponent<PlayerStatPresenter>();
        var _playerSkillHandler = GetComponent<PlayerSkillHandler>();

        _ctx = new PlayerStateContext
        {
            EnemyTransform = null,
            PlayerTransform = transform,
            Agent = agent,
            StatPresenter = statPresenter,
            Animator = animator,
            AttackEffectPrefab = attackEffectPrefab,
            playerSkillHandler = _playerSkillHandler,
            AngularTime = angularTime,
            StopAngle = stopAngle,
        };
        _ctx.Initialize();
        _ctx.SetStateChangeCallback(OnRequestStateChange);

        _states = new Dictionary<PlayerStateType, IPlayerState>
        {
            { PlayerStateType.Idle, new PlayerStateIdle() },
            { PlayerStateType.Chase, new PlayerStateChase() },
            { PlayerStateType.Attack, new PlayerStateAttack() },
            { PlayerStateType.Move, new PlayerStateMove() },
            { PlayerStateType.Die, new PlayerStateDie() }
        };

        playerStateMachine = new StateMachine<PlayerStateContext, IPlayerState, PlayerStateType>(_ctx, _states);
    }

    private void OnDisable()
    {
        _ctx.ObjDisable();
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => CharacterStatManager.Instance != null);
        yield return new WaitUntil(() => CharacterStatManager.Instance.TableLoad);

        init();
        CharacterStatManager playerStat = _ctx.StatPresenter?.PlayerStat;

        PlayerStatView statView = FindAnyObjectByType<PlayerStatView>();
        if (statView != null)
        {
            statView.InitContext(_ctx);
        }

        if (playerStat != null && _ctx.Agent != null)
        {
            _ctx.Agent.speed = playerStat.FinalMoveSpeed;
            _ctx.Agent.stoppingDistance = 1.5f;
        }
        if (DataManager.Instance.DataLoad)
        {
            _ctx.playerSkillHandler.Init(new int[] { 4000001, 4000002, 4000003 });//임시로 스킬초기화, 나중엔 ui에 장착한 스킬이나 초기스킬로.
        }
        else
        {
            DataManager.Instance.OnComplete += OnDataLoaded;
        }
        // 스폰되면 바로 chase 상태로 전환
        playerStateMachine.ChangeState(PlayerStateType.Idle);

        IsComplete = true;
    }
    private void OnDataLoaded()
    {
        DataManager.Instance.OnComplete -= OnDataLoaded;
        _ctx.playerSkillHandler.Init(new int[] { 4000001, 4000002, 4000003 });
    }
    private void Update()
    {
        if (!IsComplete)
        {
            return;
        }

        CurrentType = playerStateMachine.CurrentStateType;
        if (playerStateMachine.Current == null) return;
        _ctx.UpdateRegen(Time.deltaTime);
        playerStateMachine.Current.OnUpdate(_ctx);
    }

    private void OnRequestStateChange(PlayerStateType next)
    {
        playerStateMachine.ChangeState(next);
    }

    public void TakeDamage(float damage, DamageType damageType)
    {
        _ctx.TakeDamage(damage, damageType);
    }

}
