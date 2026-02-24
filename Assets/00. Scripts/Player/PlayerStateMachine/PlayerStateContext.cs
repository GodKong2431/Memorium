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

    public float CurrentCritMult { get; private set; }

    public float CurrentMoveSpeed => StatPresenter?.PlayerStat?.FinalMoveSpeed ?? 9f;

    public bool isGoal = false;

    public float MaxHealth => StatPresenter?.PlayerStat?.FinalHP ?? 100f;
    public float MaxMana => StatPresenter?.PlayerStat?.FinalMP ?? 100f;

    // 일반공격 사거리
    public float AttackRange => 2f;
    
    // 스킬공격 사거리
    public float FirstSkillRange => 2.5f;
    public float SecondSkillRange => 2.0f;
    public float ThirdSkillRange => 3.0f;

    private Action<PlayerStateType> _requestStateChange;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnManaChanged;

    public GameObject AttackEffectPrefab { get; set; }

    public PlayerSkillHandler playerSkillHandler;

    public void Initialize(float? startHealth = null, float? startMana = null)
    {
        CurrentHealth = startHealth ?? MaxHealth;
        CurrentMana = startMana ?? MaxMana;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);

        StatPresenter.PlayerStat.StatUpdate += SetSpeed;
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
    public void ConsumeMana(float amount)
    {
        CurrentMana -= amount;
        if (CurrentMana < 0) CurrentMana = 0;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
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

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void RestoreMana(float amount)
    {
        CurrentMana += amount;
        if (CurrentMana > MaxMana)
        {
            CurrentMana = MaxMana;
        }

        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public void RefreshMaxStats()
    {
        if (CurrentHealth > MaxHealth) CurrentHealth = MaxHealth;
        if (CurrentMana > MaxMana) CurrentMana = MaxMana;
    }

    public void SetSpeed()
    {
        Agent.speed = CurrentMoveSpeed;
    }

    public void SetCritMult(float critMult)
    {
        CurrentCritMult = critMult;
    }
}
