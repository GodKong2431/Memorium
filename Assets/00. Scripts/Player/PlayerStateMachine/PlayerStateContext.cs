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
    public EffectController EffectController { get; set; }

    public float AngularTime { get; set; }
    public float StopAngle { get; set; }

    public bool IsInvincible { get; private set; }
    public float CurrentHealth { get; private set; }
    public float CurrentMana { get; private set; }

    public float CurrentCritMult { get; private set; }

    public float CurrentMoveSpeed => StatPresenter?.PlayerStat?.FinalStats[StatType.MOVE_SPEED].finalStat ?? 9f;

    public bool isGoal = false;

    public float MaxHealth => StatPresenter?.PlayerStat?.FinalStats[StatType.HP].finalStat ?? 100f;
    public float MaxMana => StatPresenter?.PlayerStat?.FinalStats[StatType.MP].finalStat ?? 100f;

    // 일반공격 사거리
    public float AttackRange;
    
    // 스킬공격 사거리
    public float FirstSkillRange => 2.5f;
    public float SecondSkillRange => 2.0f;
    public float ThirdSkillRange => 3.0f;

    private Action<PlayerStateType> _requestStateChange;

    public event Action<float, float> OnHealthChanged;
    public event Action<float, float> OnManaChanged;

    public GameObject AttackEffectPrefab { get; set; }

    public PlayerSkillHandler playerSkillHandler;
    //초당 회복
    private float regenTimer = 0f;
    
    public float NextAttackTime;
    
    private const float REGEN_INTERVAL = 1f;
    public void Initialize(float? startHealth = null, float? startMana = null)
    {
        CurrentHealth = startHealth ?? MaxHealth;
        CurrentMana = startMana ?? MaxMana;

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);

        StatPresenter.PlayerStat.StatUpdate += SetSpeed;
    }

    public void ObjDisable()
    {
        StatPresenter.PlayerStat.StatUpdate -= SetSpeed;
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
            resistance = StatPresenter?.PlayerStat?.FinalStats[StatType.PHYS_DEF].finalStat ?? 0f;
        else if (damageType == DamageType.Magic)
            resistance = StatPresenter?.PlayerStat?.FinalStats[StatType.MAGIC_DEF].finalStat ?? 0f;

        resistance = Mathf.Clamp(resistance, 0f, 70f);
        damage *= (1f - resistance * 0.01f);

        CurrentHealth -= damage;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
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

        OnManaChanged?.Invoke(CurrentMana, MaxMana);
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

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    /// <summary>최대치가 증가했을 때(예: 버서커 모드 시작) 현재 HP/MP를 새 최대값으로 채움.</summary>
    public void SetHealthAndManaToMax()
    {
        CurrentHealth = MaxHealth;
        CurrentMana = MaxMana;
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        OnManaChanged?.Invoke(CurrentMana, MaxMana);
    }

    public void SetSpeed()
    {
        Agent.speed = CurrentMoveSpeed;
    }

    public void SetCritMult(float critMult)
    {
        CurrentCritMult = critMult;
    }
    public void UpdateRegen(float deltaTime)
    {
        regenTimer += deltaTime;
        if (regenTimer >= REGEN_INTERVAL)
        {
            regenTimer -= REGEN_INTERVAL;

            float hpRegen = StatPresenter?.PlayerStat?.FinalStats[StatType.HP_REGEN].finalStat ?? 0f;
            float mpRegen = StatPresenter?.PlayerStat?.FinalStats[StatType.MP_REGEN].finalStat ?? 0f;

            Heal(hpRegen);
            RestoreMana(mpRegen);
        }
    }
}
