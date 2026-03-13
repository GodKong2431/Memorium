using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(PlayerStatPresenter))]
[RequireComponent(typeof(PlayerSkillHandler))]
[RequireComponent(typeof(EffectController))]
[RequireComponent(typeof(PixieSpawner))]
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
    public bool IsAlive => CurrentType != PlayerStateType.Die;
    public bool isMoving => _ctx.Agent.velocity.sqrMagnitude > 0.1f;
    private StateMachine<PlayerStateContext, IPlayerState, PlayerStateType> playerStateMachine;

    bool IsComplete = false;

    private void init()
    {
        var agent = GetComponent<NavMeshAgent>();
        var statPresenter = GetComponent<PlayerStatPresenter>();
        var _playerSkillHandler = GetComponent<PlayerSkillHandler>();
        var effectController =GetComponent<EffectController>();

        CharacterStatManager.Instance.RegisterEffectController(effectController);

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
            EffectController = effectController,
        };
        _ctx.Initialize();
        _ctx.SetStateChangeCallback(OnRequestStateChange);

        BerserkerModeController.OnBerserkerModeChanged += OnBerserkerModeChanged;


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
            _ctx.Agent.speed = playerStat.FinalStats[StatType.MOVE_SPEED].finalStat;
            _ctx.Agent.stoppingDistance = 1.5f;
        }
        if (DataManager.Instance.DataLoad)
        {
            _ctx.playerSkillHandler.InitFromPreset();


            PixieSpawnTest();//TO DO: UI 연동시 삭제 할것
        }
        else
        {
            DataManager.Instance.OnComplete += OnDataLoaded;
        }
        // 스폰되면 바로 chase 상태로 전환
        playerStateMachine.ChangeState(PlayerStateType.Idle);

        IsComplete = true;
    }

    [ContextMenu("픽시소환")]
    public void PixieSpawnTest()//TO DO:UI 연동시 삭제
    {
        InventoryManager.Instance.AddItem(3310001, 100);

        var pixieModule = InventoryManager.Instance.GetModule<PixieInventoryModule>();
        if (pixieModule != null)
        {
            int targetFairyID = 5000001;
            bool isUnlocked = pixieModule.TryUnlockPixie(targetFairyID);

            if (isUnlocked)
            {
                pixieModule.EquipPixie(targetFairyID);
                Debug.Log("픽시");
            }
        }
    }
    private void OnDataLoaded()
    {
        DataManager.Instance.OnComplete -= OnDataLoaded;
        _ctx.playerSkillHandler.InitFromPreset();
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

    private void OnBerserkerModeStarted()
    {
        if (_ctx != null)
            _ctx.SetHealthAndManaToMax();
    }

    private void OnBerserkerModeEnded()
    {
        if (_ctx != null)
            _ctx.RefreshMaxStats();
    }
    
    private void OnBerserkerModeChanged(bool berserkerState)
    {
        if (_ctx != null)
        {
            if (berserkerState)
            {
                _ctx.SetHealthAndManaToMax();
            }
            
            else
            {
                _ctx.RefreshMaxStats();
            }
        }
    }

    private void OnDestroy()
    {
        BerserkerModeController.OnBerserkerModeChanged -= OnBerserkerModeChanged;
    }

    public void TakeDamage(float damage, DamageType damageType)
    {
        _ctx.TakeDamage(damage, damageType);
    }


}
