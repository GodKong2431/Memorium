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

public class PlayerSkillHandler :MonoBehaviour, ISkillStatProvider, ISkillTargetProvider
{
    private SkillCaster skillCaster;
    private CharacterStatManager characterStatManager;
    private SkillDataContext[] skilldataContexts;
    private float[] cooldownTimers;
    private PlayerStateMachine playerStateMachine;

    public int SkillCount => skilldataContexts.Length;

    private void Awake()
    {
        skillCaster = GetComponent<SkillCaster>();
        characterStatManager = GetComponent<CharacterStatManager>();
        playerStateMachine = GetComponent<PlayerStateMachine>();
    }
    public void Init(int[] skillIDs, int[] m4IDs =null, int[] m5IDs=null )
    {
        skilldataContexts = new SkillDataContext[skillIDs.Length];
        cooldownTimers = new float[skillIDs.Length];
        for(int i=0; i< skillIDs.Length; i++)
        {
            skilldataContexts[i] = new SkillDataContext(skillIDs[i], m4IDs?[i] ?? -1, m5IDs?[i] ?? -1);
            Debug.Log($"슬롯{i}: skillData={skilldataContexts[i].skillData != null}, table={skilldataContexts[i].skillData?.skillTable != null}");
            cooldownTimers[i] = 0;
           
        }
        skillCaster.Init(this,this,SetInvincible);

    }
    public void SetSkillContext(int index, int skillID, int m4ID = -1, int m5ID = -1)
    {
        if (index < 0 || index >= skilldataContexts.Length) return;
        skilldataContexts[index] = new SkillDataContext(skillID, m4ID, m5ID);
        cooldownTimers[index] = 0;
    }

    private void Update()
    {
        CooldownLoop();
    }

    private void CooldownLoop()
    {
        if(skilldataContexts == null) return;
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
        float cost = skilldataContexts[index].skillData.skillTable.skillDamageValue;
        return playerStateMachine._ctx.CurrentMana >= cost;
    }
    public bool AutoCast()
    {
        if (skilldataContexts == null) return false;
        if (skillCaster.IsCasting) return false;

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
        if (!CheckCooldown(index)) return false;
        if (!CheckRange(index, distToTarget)) return false;
        if (!CheckMana(index)) return false;

        playerStateMachine._ctx.ConsumeMana(skilldataContexts[index].skillData.skillTable.skillDamageValue);// 마나소모량으로 바꿔야함
        skillCaster.CastSkill(skilldataContexts[index]);
        cooldownTimers[index] = skilldataContexts[index].skillData.skillTable.skillCooldown;
        return true;
    }
    public bool ReadySkill(float dist)
    {
        if (skilldataContexts == null) return false;
        for (int i = 0; i < skilldataContexts.Length; i++)
        {
            if (CheckCooldown(i) && CheckRange(i, dist) && CheckMana(i))
                return true;
        }
        return false;
    }
    public float GetAttack() =>characterStatManager.FinalATK;

    public float GetCriticalChance() => characterStatManager.FinalCritChance;

    public float GetCriticalMulti() => characterStatManager.FinalCritMult;


    public Transform GetTarget()
    {
        var enemy = EnemyTarget.GetTarget(transform.position);
        return enemy != null ? enemy.transform : null;
    }

    public void SetInvincible(bool active)
    {
        playerStateMachine._ctx.SetInvincibility(active);
    }
}