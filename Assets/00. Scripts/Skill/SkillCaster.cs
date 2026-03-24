using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 스킬 실행하는 컴포넌트, 플레이어/몬스터/분신 어디든 붙여도 나가도록
/// </summary>
public class SkillCaster : MonoBehaviour, ISkillCasterMovement, ISkillHitHandler, ISkillDetectable
{
    private ISkillStatProvider statProvider;
    private ISkillTargetProvider targetProvider;

    [Header("레이어")]
    [SerializeField] private LayerMask targetLayer; 

    [Header("테스트")]
    [SerializeField] SkillProjectile projectilePrefab;
    [SerializeField] SkillDeploy deployPrefab;
    [SerializeField] GameObject auraPrefab;
    [SerializeField] GameObject shadowPrepab;
    [SerializeField] FireZone fireZonePrefab;
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

        isCasting = true;
        skillDataContext = dataContext;

        if (applyAddon)
            dataContext.ResetAddonState();
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
        //agent.Warp(transform.position);
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
                var module = InventoryManager.Instance.GetModule<SkillInventoryModule>();
                {
                    var OwnedSKill = module.GetSkillData(data.skillData.skillTable.ID);
                    var skillGrade = OwnedSKill.GetGrade();
                    float level = OwnedSKill.level;
                    float damage = data.skillData.skillTable.skillDamage + statProvider.GetAttack() * (1 + data.skillData.skillTable.skillDamageValue) * (1 + (0.1f * level));
                    if (skillGrade == SkillGrade.Mythic)
                        damage *= 1.5f;
                    target.TakeDamage(damage);
                }

                if(applyAddon)
                    PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(effectPath, target.transform, true));
                else
                    PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(data.m4Data.m4VFX2, target.transform, true));

                Debug.Log($"[ProcessHit] applyAddon={applyAddon}, m4Strategy={m4Strategy?.GetType().Name ?? "NULL"}");
                if (applyAddon && m4Strategy is ISkillHitAddon hitAddon)
                {
                    Debug.Log("[ProcessHit] AddonImpact.OnHit 호출");
                    hitAddon.OnHit(this, target.transform, data, targetLayer);
                }

                if (hitBuffer[i].TryGetComponent<EffectController>(out var controller))
                {
                    ApplyM5Effect(data, controller, target.transform.position);
                }
            }
        }
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
            Debug.Log("화염 소환");
            var obj = PoolAddressableManager.Instance.GetPooledObject(fireZonePath,hitPos,Quaternion.identity);
            if (obj != null)
            {
                if (obj.TryGetComponent<FireZone>(out var fireZone))
                    fireZone.Init(m5A, 3.0f, targetLayer);
            }
        }
        else
        {
            Debug.Log("상태이상 적용");
            var effect = StatusEffectFactory.Create(activeData);
            if (effect != null&& !controller.HasStatusEffect()) 
                controller.ApplyStatusEffect(effect);
        }
    }

}
