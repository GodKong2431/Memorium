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
    private CharacterStatManager finalStat;
    private SkillDataContext[] skilldataContexts;
    private float[] cooldownTimers;

    public int SkillCount => skilldataContexts.Length;

    private void Awake()
    {
        skillCaster = GetComponent<SkillCaster>();
        finalStat = GetComponent<CharacterStatManager>();
    }
    public void Init(int[] skillIDs, int[] m4IDs =null, int[] m5IDs=null )
    {
        skilldataContexts = new SkillDataContext[skillIDs.Length];
        cooldownTimers = new float[skillIDs.Length];
        for(int i=0; i< skillIDs.Length; i++)
        {
            skilldataContexts[i] = new SkillDataContext(skillIDs[i], m4IDs?[i] ?? -1, m5IDs?[i] ?? -1);
            Debug.Log($"½½·Ô{i}: skillData={skilldataContexts[i].skillData != null}, table={skilldataContexts[i].skillData?.skillTable != null}");
            cooldownTimers[i] = 0;
           
        }
        skillCaster.Init(this,this);

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
        if (cooldownTimers == null) return false;
        if (cooldownTimers[index] > 0f) return false;
        if (distToTarget > skilldataContexts[index].skillData.skillTable.skillRange) return false;

        skillCaster.CastSkill(skilldataContexts[index]);
        cooldownTimers[index] = skilldataContexts[index].skillData.skillTable.skillCooldown;
        return true;
    }
    public bool ReadySkillInRange(float dist)
    {
        if (skilldataContexts == null) return false;
        if (cooldownTimers == null) return false;
        for (int i = 0; i < skilldataContexts.Length; i++)
        {
            if (skilldataContexts[i]?.skillData?.skillTable == null) continue;
            if (cooldownTimers[i] <= 0f && dist <= skilldataContexts[i].skillData.skillTable.skillRange)
                return true;
        }
        return false;
    }
    public float GetAttack() =>finalStat.FinalATK;


    public float GetCriticalChance() => finalStat.FinalCritChance;

    public float GetCriticalMulti() => finalStat.FinalCritMult;

    public Transform GetTarget()
    {
        var enemy = EnemyTarget.GetTarget(transform.position);
        return enemy != null ? enemy.transform : null;
    }
}