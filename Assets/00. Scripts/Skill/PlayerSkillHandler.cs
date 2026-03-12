using System;
using Unity.Mathematics;
using UnityEngine;

public interface ISkillStatProvider
{
    float GetAttack();
    float GetCriticalChance();
    float GetCriticalMulti();
}

public interface ISkillTargetProvider
{
    Transform GetTarget();
}
public interface ISkillCooldownProvider
{
    float GetCooldownRemain(int index);
    float GetCooldownMax(int index);
}

public class PlayerSkillHandler : MonoBehaviour, ISkillStatProvider, ISkillTargetProvider, ISkillCooldownProvider
{
    [SerializeField] private BattleSkillPresenter battleSkillPresenter;

    private SkillCaster skillCaster;
    private SkillDataContext[] skilldataContexts;
    private float[] cooldownTimers;
    private float[] cooldownTimeMax;
    private PlayerStateMachine playerStateMachine;
    private SkillInventoryModule subscribedSkillModule;

    public int SkillCount => skilldataContexts?.Length ?? 0;

    public bool IsCasting() => skillCaster != null && skillCaster.IsCasting();
    public bool IsChanneling() => skillCaster != null && skillCaster.IsChanneling();

    private void Awake()
    {
        skillCaster = GetComponent<SkillCaster>();
        playerStateMachine = GetComponent<PlayerStateMachine>();
    }

    private void Start()
    {
        if (DataManager.Instance.DataLoad)
        {
            EnsureSkillModuleSubscription();
            InitFromPreset();
        }
        else
        {
            DataManager.Instance.OnComplete += OnDataLoaded;
        }
    }
    
    private void OnDataLoaded()
    {
        DataManager.Instance.OnComplete -= OnDataLoaded;
        EnsureSkillModuleSubscription();
        InitFromPreset();
    }
    public void Init(SkillPresetSlot[] slots)
    {
        skilldataContexts = new SkillDataContext[slots.Length];
        cooldownTimers = new float[slots.Length];
        cooldownTimeMax = new float[slots.Length];
        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            skilldataContexts[i] = new SkillDataContext(
                s.skillID, s.m4JemID, s.m5JemIDs[0], s.m5JemIDs[1]
            );
            cooldownTimers[i] = 0;
            cooldownTimeMax[i] = 0;
        }
        skillCaster.Init(this, this, SetInvincible);
        battleSkillPresenter?.BindCooldownProvider(this);
    }
    private void OnEnable()
    {
        EnsureSkillModuleSubscription();
    }

    private void OnDisable()
    {
        UnsubscribeSkillModule();
    }

    private void OnPresetChanged(int presetIndex)
    {
        InitFromPreset();
    }
    public void InitFromPreset()
    {
        var skillModule = InventoryManager.Instance?.GetModule<SkillInventoryModule>();
        if (skillModule == null) return;
        Init(skillModule.GetCurrentPreset().slots);
    }


    // 이거 왜만들었던 건지 까먹음
    public void SetSkillContext(int index, int skillID, int m4ID = -1, int m5IDa = -1, int m5IDb =-1)
    {
        if (index < 0 || index >= skilldataContexts.Length) return;
        skilldataContexts[index] = new SkillDataContext(skillID, m4ID, m5IDa, m5IDb);
        cooldownTimers[index] = 0;
        cooldownTimeMax[index] = 0;
    }

    private void Update()
    {
        EnsureSkillModuleSubscription();
        CooldownLoop();
    }

    private void EnsureSkillModuleSubscription()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null || subscribedSkillModule == skillModule)
            return;

        UnsubscribeSkillModule();
        subscribedSkillModule = skillModule;
        subscribedSkillModule.OnPresetChanged += OnPresetChanged;

        if (DataManager.Instance != null && DataManager.Instance.DataLoad)
            InitFromPreset();
    }

    private void UnsubscribeSkillModule()
    {
        if (subscribedSkillModule == null)
            return;

        subscribedSkillModule.OnPresetChanged -= OnPresetChanged;
        subscribedSkillModule = null;
    }

    private void CooldownLoop()
    {
        if (skilldataContexts == null) return;
        if (cooldownTimers == null) return;
        float deltaTime = Time.deltaTime;
        for (int i = 0; i < skilldataContexts.Length; i++)
        {
            if (cooldownTimers[i] > 0f)
            {
                cooldownTimers[i] = Mathf.Max(0f, cooldownTimers[i] - deltaTime);
            }
        }
    }

    public bool AutoCast()
    {
        if (skilldataContexts == null) return false;
        if (skillCaster.IsCasting()) return false;

        var enemy = EnemyTarget.GetTarget(transform.position);
        if (enemy == null) return false;

        float dist = Vector3.Distance(transform.position, enemy.transform.position);

        for (int i = 0; i < skilldataContexts.Length; i++)
        {
            if (TryCastSkill(i, dist)) return true;
        }
        return false;
    }
    public bool TryCastSkill(int index, float distToTarget)
    {
        if (skilldataContexts == null) return false;
        if (index < 0 || index >= skilldataContexts.Length) return false;
        if (!ReadySkill(index, distToTarget)) return false;

        playerStateMachine._ctx.ConsumeMana(skilldataContexts[index].skillData.skillTable.manaCost);
        skillCaster.CastSkill(skilldataContexts[index]);
        float cooldownReduce = CharacterStatManager.Instance.GetFinalStat(StatType.COOLDOWN_REDUCE);
        float maxCooldown = Mathf.Max(0f, skilldataContexts[index].skillData.skillTable.skillCooldown * (1f - cooldownReduce * 0.01f));
        cooldownTimers[index] = maxCooldown;
        cooldownTimeMax[index] = maxCooldown;
        return true;
    }
    public bool ReadySkill(float dist)
    {
        if (skilldataContexts == null) return false;
        for (int i = 0; i < skilldataContexts.Length; i++)
        {
            if (ReadySkill(i, dist)) return true;
        }
        return false;
    }
    private bool ReadySkill(int index, float dist)
    {
        return CheckCooldown(index) && CheckRange(index, dist) && CheckMana(index);
    }
    private bool CheckCooldown(int index)
    {
        if (cooldownTimers == null) return false;
        return cooldownTimers[index] <= 0f;
    }

    private bool CheckRange(int index, float dist)
    {
        if (skilldataContexts[index]?.skillData?.skillTable == null) return false;
        return dist <= skilldataContexts[index].skillData.skillTable.skillRange;
    }

    private bool CheckMana(int index)
    {
        if (skilldataContexts[index]?.skillData?.skillTable == null) return false;
        float cost = skilldataContexts[index].skillData.skillTable.manaCost;
        return playerStateMachine._ctx.CurrentMana >= cost;

    }

    #region ISkillCooldownProvider

    public float GetCooldownRemain(int index)
    {
        if (cooldownTimers == null || index < 0 || index >= cooldownTimers.Length) return 0f;
        return cooldownTimers[index];
    }

    public float GetCooldownMax(int index)
    {
        if (cooldownTimeMax == null || index < 0 || index >= cooldownTimeMax.Length) return 0f;
        return cooldownTimeMax[index];
    }

    #endregion


    #region ISkillStatProvider
    public float GetAttack() => CharacterStatManager.Instance.GetFinalStat(StatType.ATK);

    public float GetCriticalChance() => CharacterStatManager.Instance.GetFinalStat(StatType.CRIT_CHANCE);

    public float GetCriticalMulti() => CharacterStatManager.Instance.GetFinalStat(StatType.CRIT_MULT);
    #endregion


    #region ISkillTargetProvider
    public Transform GetTarget()
    {
        var enemy = EnemyTarget.GetTarget(transform.position);
        return enemy != null ? enemy.transform : null;
    }
    #endregion

    public void SetInvincible(bool active)
    {
        playerStateMachine._ctx.SetInvincibility(active);
    }
}
