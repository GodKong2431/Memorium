using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateContext : BaseStateContext
{
    public Transform PlayerTransform { get; set; }
    public Transform EnemyTransform { get; set; }
    public NavMeshAgent Agent { get; set; }
    public Presenter StatPresenter { get; set; }
    public Animator Animator { get; set; }

    public float CurrentHealth { get; private set; }

    public bool isFirstSkillReady { get; set; }
    public bool isSecondSkillReady { get; set; }
    public bool isThirdSkillReady { get; set; }

    public float MaxHealth => StatPresenter?.Data?.baseHP ?? 100f;

    public float AttackRange => 1.5f;

    public float FirstSkillRange => 2.5f;
    public float SecondSkillRange => 2.0f;

    public float ThirdSkillRange => 3.0f;

    private Action<PlayerStateType> _requestStateChange;

    public void Initialize(float? startHealth = null)
    {
        CurrentHealth = startHealth ?? MaxHealth;
    }

    public void SetStateChangeCallback(Action<PlayerStateType> callback)
    {
        _requestStateChange = callback;
    }

    public void RequestState(PlayerStateType next)
    {
        _requestStateChange?.Invoke(next);
    }
}
