using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerStatPresenter))]
public class PlayerStateMachine : MonoBehaviour
{
    [SerializeField]
    [Tooltip("현재 적 개체가 사용할 애니메이터입니다.")]
    private Animator animator; // 애니메이터는 현재 미구현, 향후 추가를 위해 직렬화만 해둠

    [SerializeField]
    [Tooltip("공격 시 나타나는 이펙트 프리팹입니다.")]
    private GameObject attackEffectPrefab;

    PlayerStateContext _ctx;
    private Dictionary<PlayerStateType, IPlayerState> _states;
    //private IPlayerState _current;
    //private PlayerStateType _currentType;

    public PlayerStateType CurrentType;

    private StateMachine<PlayerStateContext, IPlayerState, PlayerStateType> playerStateMachine;

    private void Awake()
    {
        var agent = GetComponent<NavMeshAgent>();
        var statPresenter = GetComponent<PlayerStatPresenter>();

        _ctx = new PlayerStateContext
        {
            EnemyTransform = null,
            PlayerTransform = transform,
            Agent = agent,
            StatPresenter = statPresenter,
            Animator = animator,
            AttackEffectPrefab = attackEffectPrefab
        };
        _ctx.Initialize();
        _ctx.SetStateChangeCallback(OnRequestStateChange);

        _states = new Dictionary<PlayerStateType, IPlayerState>
        {
            { PlayerStateType.Idle, new PlayerStateIdle() },
            { PlayerStateType.Chase, new PlayerStateChase() },
            { PlayerStateType.Attack, new PlayerStateAttack() },
            { PlayerStateType.Move, new PlayerStateMove() },
        };

        playerStateMachine = new StateMachine<PlayerStateContext, IPlayerState, PlayerStateType>(_ctx, _states);
    }

    private void Start()
    {
        CharacterBaseStatInfoTable data = _ctx.StatPresenter?.Data;

        if (data != null && _ctx.Agent != null)
        {
            _ctx.Agent.speed = data.baseMoveSpeed;
            _ctx.Agent.stoppingDistance = 1.5f;
        }

        // 스폰되면 바로 chase 상태로 전환
        playerStateMachine.ChangeState(PlayerStateType.Idle);
    }

    private void Update()
    {
        CurrentType = playerStateMachine.CurrentStateType;
        if (playerStateMachine.Current == null) return;
        playerStateMachine.Current.OnUpdate(_ctx);
    }

    private void OnRequestStateChange(PlayerStateType next)
    {
        playerStateMachine.ChangeState(next);
    }
}
