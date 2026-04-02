using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 스킬 실행하는 컴포넌트, 플레이어/몬스터/분신 어디든 붙여도 나가도록
/// </summary>
public class SkillCaster : MonoBehaviour, ISkillCasterMovement, ISkillHitHandler, ISkillDetectable
{
    private ISkillStatProvider statProvider;
    private ISkillTargetProvider targetProvider;

    [Header("레이어")]
    [SerializeField] private LayerMask targetLayer; 

    [Header("이펙트 경로")]
    [SerializeField] string effectPath = "Assets/02. Prefabs/SKill/HCFX_Hit_08.prefab";

    [SerializeField] string projectilePath= "Assets/02. Prefabs/SKill/Projectile/bullet.prefab";
    [SerializeField] string deployPath= "Assets/02. Prefabs/SKill/Deploy/Deploy.prefab";
    [SerializeField] string auraPath= "Assets/02. Prefabs/SKill/Aura/Aura.prefab";
    [SerializeField] string shadowPath= "Assets/02. Prefabs/SKill/Shadow/Shadow.prefab";
    [SerializeField] string fireZonePath= "Assets/02. Prefabs/SKill/FireZone.prefab";

    private SkillDataContext skillDataContext;
    private bool isCasting = false;
    private bool isChanneling = false;
    public bool isShadow=false; //개씹하드코코딩
    public bool IsCasting() => isCasting;
    public bool IsChanneling() => isChanneling;
    private Coroutine currentSkillRoutine;

    private Action<bool> onInvincibleChanged;       

    private Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];//타격 대상 버퍼, nonalloc 저장용도

    private Collider[] cachedTargets = new Collider[SkillConstants.HIT_BUFFER_SIZE];//스플래시용

    // 기즈모 디버그용
    private SkillData debugLastSkillData;
    private Vector3 debugLastCastPos;
    private Vector3 debugLastCastDir;

    private Vector3 castPostion;
    private Vector3 castDirection;

    public Vector3 CastPosition => castPostion;//스킬 시전 위치 저장용
    public Vector3 CastDirection => castDirection;//스킬 시전 방향 저장용

    private Vector3 castTargetPosition;
    public Vector3 CastTargetPosition => castTargetPosition;

    public Vector3 Position => transform.position;
    public event Action OnSkillEnd;
    private NavMeshAgent agent; 

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }
    public void SetPosition(Vector3 position)
    {
        if (position == transform.position) return;
            transform.position = position;
    }

    public void SetInvincible(bool active)
    {
        onInvincibleChanged?.Invoke(active);
    }

    public void SetChanneling(bool active)
    {
        isChanneling = active;
    }


    public void PlayAnim(string key)
    {

    }

    public void Init(ISkillStatProvider stat, ISkillTargetProvider target, Action<bool> onInvincible = null)
    {
        statProvider = stat;
        targetProvider = target;
        onInvincibleChanged = onInvincible;
    }
    public Vector3 GetTargetPosition()
    {
        if (isShadow) return castTargetPosition;
        if (targetProvider != null)
        {
            Transform target = targetProvider.GetTarget();
            if (target != null) return target.position;
        }
        return transform.position + transform.forward;
    }
    public Vector3 GetTargetDirection()
    {
        if (isShadow) return castDirection;
        if (targetProvider != null)
        {
            Transform target = targetProvider.GetTarget();
            if (target != null)
            {
                Vector3 dir = target.position - transform.position;
                dir.y = 0;
                return dir.normalized;
            }
        }
        return transform.forward;
    }

    public SkillDataContext ResetContext(int skillID, int m4ID = -1, int m5ID = -1)
    {
        if (skillDataContext == null)
            skillDataContext = new SkillDataContext(skillID, m4ID, m5ID);
        else
            skillDataContext.SetSkillContext(skillID, m4ID, m5ID);
        return skillDataContext;
    }
    public void CastSkill(SkillDataContext dataContext, float extraDelay = 0, bool applyAddon = true)
    {
        if (isCasting || isChanneling) return;
        if (dataContext == null) return;

        isCasting = true;
        if (skillDataContext == null)
            skillDataContext = new SkillDataContext(dataContext.skillData.skillTable.ID,-1,-1,-1);
        skillDataContext.CopyFrom(dataContext);

        if (!applyAddon)
            skillDataContext.ClearAddon();
        if (currentSkillRoutine != null)
            StopCoroutine(currentSkillRoutine);
        if (!isShadow)
        {
            CacheCastState();
        }
        PreLoadSKillPrefab(dataContext);

        currentSkillRoutine = StartCoroutine(SkillSequence(skillDataContext, extraDelay));
    }
    private void PreLoadSKillPrefab(SkillDataContext dataContext)
    {
        PoolableParticleManager.Instance.Preload(dataContext?.skillData?.skillTable?.skillVFX);
        PoolableParticleManager.Instance.Preload(dataContext?.m4Data?.m4VFX);
        PoolableParticleManager.Instance.Preload(dataContext?.m4Data?.m4VFX2);
        PoolableParticleManager.Instance.Preload(dataContext?.m5DataA?.m5VFX);
        PoolableParticleManager.Instance.Preload(dataContext?.m5DataA?.m5VFX2);
        PoolableParticleManager.Instance.Preload(dataContext?.m5DataB?.m5VFX);
        PoolableParticleManager.Instance.Preload(dataContext?.m5DataB?.m5VFX2);

        PoolAddressableManager.Instance.Preload(projectilePath);
        PoolAddressableManager.Instance.Preload(deployPath);
        PoolAddressableManager.Instance.Preload(fireZonePath);
        PoolAddressableManager.Instance.Preload(auraPath);
        PoolAddressableManager.Instance.Preload(shadowPath);
    }

    /// <summary>
    ///  시전위치 방향정보 저장, Shadow에 사용
    /// </summary>
    private void CacheCastState()
    {
        castPostion = transform.position;
        castDirection = GetTargetDirection();
        castTargetPosition = GetTargetPosition();
    }
    public void SetShadowData(Vector3 targetPos, Vector3 dir)
    {
        isShadow = true;
        castTargetPosition = targetPos;
        castDirection = dir;
    }
    private IEnumerator SkillSequence(SkillDataContext dataContext, float extraDelay = 0)
    {
        if (dataContext == null) yield break;
        SkillData data = dataContext.skillData;
        debugLastSkillData = data;
        if(extraDelay > 0)
        {
            yield return CoroutineManager.waitForSeconds(extraDelay);
        }
        SoundManager.Instance.PlayCombatSfx(data.skillTable.skillSound);
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(data.skillTable.skillVFX, transform, true,rotation: transform.rotation));
        yield return SkillSequenceMove(data);
        ResetAgentWarp();

        debugLastCastPos = transform.position + debugLastCastDir * dataContext.skillData.m3Data.m3Distance;
        yield return SkillSequenceExecute(dataContext);

        SkillEnd();
    }

    private void ResetAgentWarp()
    {
        if (agent == null || !agent.isActiveAndEnabled) return;

        //agent.ResetPath();
        agent.Warp(transform.position);
    }

    /// <summary>
    /// 모듈 1 시퀀스
    /// </summary>
    private IEnumerator SkillSequenceMove(SkillData data)
    {
        Vector3 targetPosition = GetTargetPosition();
        debugLastCastDir = GetTargetDirection();
        if (agent != null)
        {
            agent.isStopped = true;
            agent.updateRotation = false;
        }
        if (data.m1Data.m1Delay > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m1Data.m1Delay);
        }
        var m1Strategy = SkillStrategyContainer.GetMovement(data.m1Data.m1Type);

        yield return m1Strategy.SkillMove(this, targetPosition, data.m1Data);
    }

    /// <summary>
    /// 모듈 3 시퀀스, 모듈2는 모듈3에 종속되어있어서 따로 시퀀스 나누지않음
    /// </summary>
    /// <param name="dataContext"></param>
    /// <returns></returns>
    private IEnumerator SkillSequenceExecute(SkillDataContext dataContext)
    {
        M3Type m3Type = dataContext.skillData.m3Data.m3Type;
        float delay = dataContext.skillData.m3Data.m3Delay;
        Vector3 executePivot = transform.position;

        if (delay > 0)
        {
            yield return CoroutineManager.waitForSeconds(delay);
        }
        var m3Strategy = SkillStrategyContainer.GetExecute(m3Type);

        //나중에 프리팹을 넘기는게아니라 데이터 테이블에서 프리팹을 가져오도록 바꿀예정

        yield return m3Strategy.Execute(this, this, dataContext, executePivot, castDirection, targetLayer);

    }

    private void SkillEnd()
    {
        isCasting = false;
        if(agent != null)
        {
            agent.isStopped = false;
            agent.updateRotation = true;
        }
        OnSkillEnd?.Invoke();
    }
    public void StopSkill()
    {
        if (currentSkillRoutine != null) StopCoroutine(currentSkillRoutine);
        SkillEnd();
    }
    private void OnDrawGizmos()
    {
        if (debugLastSkillData == null) return;

        var m2Strategy = SkillStrategyContainer.GetDetect(debugLastSkillData.m2Data.m2Type);

        Vector3 drawPos = isCasting ? transform.position : debugLastCastPos;
        Vector3 drawDir = isCasting ? transform.forward : debugLastCastDir;

        if (drawDir == Vector3.zero)
        {
            drawDir = transform.forward;
        }
        if (m2Strategy != null)
        {
            m2Strategy.DrawGizmo(drawPos, drawDir, debugLastSkillData.m2Data);
        }
    }

    public Collider[] GetBuffer()
    {
        return hitBuffer;
    }

    public Collider[] GetAddonBuffer()
    {
        return cachedTargets;
    }
    public SkillDataContext GetSkillDataContext()
    {
        return skillDataContext;
    }
    public void HandleSkillHit(int hitCount, SkillDataContext data, Collider[] hitBuffer)
    {
        ProcessHit(hitCount, data, hitBuffer, true);
    }

    public void HandleAddonHit(int hitCount, SkillDataContext data, Collider[] hitBuffer)
    {
        ProcessHit(hitCount, data, hitBuffer, false);
    }

    private void ProcessHit(int hitCount, SkillDataContext data, Collider[] hitBuffer, bool applyAddon)
    {
        if (hitCount == 0) return;
        var m4VFx2path = data?.m4Data?.m4VFX2;
        ISkillAddonStrategy m4Strategy = null;
        if (applyAddon && data.m4Data != null)
        {
            m4Strategy = SkillStrategyContainer.GetAddon(data.m4Data.m4Type);
        }

        if (m4Strategy is ISkillCastAddon castAddon && data.GetAddonTriggerCount()==0)//추후 횟수제한 스킬나오면 csv에 필드만들고 체크하는식으로
        {
            data.RecordAddonTrigger();
            castAddon.OnCast(this,this,statProvider,targetProvider, skillDataContext);
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].TryGetComponent<IDamageable>(out var target))
            {
                if (applyAddon && m4Strategy is ISkillHitAddon hitAddon)
                {
                    hitAddon.OnHit(this, target.transform, data, targetLayer);
                }
                if (!applyAddon && m4Strategy is ISkillHitAddon)
                {
                    PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(m4VFx2path, target?.transform, true));
                }
                else
                {
                    PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(effectPath, target?.transform, true));
                }

                var module = InventoryManager.Instance?.GetModule<SkillInventoryModule>();
                {
                    var ownedSkill = module?.GetSkillData(data.skillData.skillTable.ID);
                    if (ownedSkill == null || statProvider == null) continue;
                    var skillGrade = ownedSkill.GetGrade();
                    float level = ownedSkill.level;
                    float damage = data.skillData.skillTable.skillDamage + statProvider.GetAttack() * (1 + data.skillData.skillTable.skillDamageValue) * (1 + (0.1f * level));
                    if (skillGrade == SkillGrade.Mythic)
                        damage *= 1.5f;

                    bool isCritical = RollCriticalHit();
                    if (isCritical)
                        damage *= Mathf.Max(1f, statProvider.GetCriticalMulti());

                    ApplySkillDamage(target, damage, isCritical);
                }


                if (hitBuffer[i].TryGetComponent<EffectController>(out var controller))
                {
                    if (!controller.HasStatusEffect())
                        ApplyM5Effect(data, controller, target.transform.position);
                }
            }
        }
    }

    private bool RollCriticalHit()
    {
        if (statProvider == null)
            return false;

        float criticalChance = statProvider.GetCriticalChance();
        if (criticalChance <= 0f)
            return false;

        if (criticalChance >= 1f)
            return true;

        return UnityEngine.Random.value < criticalChance;
    }

    private static void ApplySkillDamage(IDamageable target, float damage, bool isCritical)
    {
        if (target == null)
            return;

        if (target is EnemyStateMachine enemyStateMachine)
        {
            enemyStateMachine.TakeDamage(damage, DamageType.Physical, isCritical);
            return;
        }

        target.TakeDamage(damage, DamageType.Physical);
    }


    private void ApplyM5Effect(SkillDataContext data, EffectController controller, Vector3 hitPos)
    {
        var m5A = data.m5DataA;
        var m5B = data.m5DataB;

        var activeData = m5A ?? m5B;
        if (activeData ==  null) return;


        if (m5A != null && m5B != null)
        {
            var effect = StatusEffectFactory.CreateFusion(m5A, m5B);
            if (effect != null&&!controller.HasStatusEffect()) 
                controller.ApplyStatusEffect(effect);
            return;
        }

        if (activeData.applyType == ApplyType.strikeLocation)
        {
            var obj = PoolAddressableManager.Instance.GetPooledObject(fireZonePath,hitPos,Quaternion.identity);
            if (obj != null)
            {
                if (obj.TryGetComponent<FireZone>(out var fireZone))
                    fireZone.Init(m5A, 3.0f, targetLayer);
            }
        }
        else
        {
            var effect = StatusEffectFactory.Create(activeData);
            if (effect != null&& !controller.HasStatusEffect()) 
                controller.ApplyStatusEffect(effect);
        }
    }

}
