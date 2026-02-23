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
    public bool IsInvincible { get; private set; }
    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }


    public bool isGoal = false;

    public float MaxHealth => StatPresenter?.PlayerStat?.FinalHP ?? 100f;
    public float MaxMana => StatPresenter?.PlayerStat?.FinalMP ?? 100f;

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
    public void TakeDamage(float damage, DamageType damageType)
    {
        if (IsInvincible) return;

        float resistance = 0f;
        if (damageType == DamageType.Physical)
            resistance = StatPresenter?.PlayerStat?.FinalPhysDEF ?? 0f;
        else if (damageType == DamageType.Magic)
            resistance = StatPresenter?.PlayerStat?.FinalMagicDEF ?? 0f;

        resistance = Mathf.Clamp(resistance, 0f, 70f);
        damage *= (1f - resistance * 0.01f);

        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            RequestState(PlayerStateType.Die);
        }
    }
    public void RequestState(PlayerStateType next)
    {
        _requestStateChange?.Invoke(next);
    }

    public void SetInvincibility(bool invincible)
    {
        IsInvincible = invincible;
    }

    public void Heal(float amount)
    {
        CurrentHealth += amount;
        if (CurrentHealth > MaxHealth)
        {
            CurrentHealth = MaxHealth;
        }
    }

    public void RestoreMana(float amount)
    {
        CurrentMana += amount;
        if (CurrentMana > MaxMana)
        {
            CurrentMana = MaxMana;
        }
    }

}
