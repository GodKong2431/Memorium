using System;
using UnityEngine;
using UnityEngine.AI;

public class PlayerStateContext : BaseStateContext
{
    public Transform PlayerTransform { get; set; }
    public Transform EnemyTransform { get; set; }
    public NavMeshAgent Agent { get; set; }
    public PlayerStatPresenter StatPresenter { get; set; }
    public Animator Animator { get; set; }

    public float CurrentHealth { get; private set; }

    public bool isFirstSkillReady = true;
    public bool isSecondSkillReady = true;
    public bool isThirdSkillReady = true;

    public float MaxHealth => StatPresenter?.playerStat?.HP ?? 100f;
    
    // 일반공격 사거리
    public float AttackRange => 1.5f;
    
    // 스킬공격 사거리
    public float FirstSkillRange => 2.5f;
    public float SecondSkillRange => 2.0f;
    public float ThirdSkillRange => 3.0f;

    private Action<PlayerStateType> _requestStateChange;

    public GameObject AttackEffectPrefab { get; set; }

    public PlayerSkillHandler playerSkillHandler;

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
