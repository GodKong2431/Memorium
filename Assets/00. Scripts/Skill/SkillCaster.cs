using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UI.GridLayoutGroup;

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
    [SerializeField] GameObject shadowPrepab;
    [SerializeField] FireZone fireZonePrefab;

    private SkillDataContext skillDataContext;
    private bool isCasting = false;
    public bool IsCasting => isCasting;
    private Coroutine currentSkillRoutine;

    private Action<bool> onInvincibleChanged;       

    private Collider[] hitBuffer = new Collider[SkillConstants.HIT_BUFFER_SIZE];//타격 대상 버퍼, nonalloc 저장용도

    // 기즈모 디버그용, 다른 시각적 방식으로 바꾸는게 좋을것 같음.
    private SkillData debugLastSkillData;
    private Vector3 debugLastCastPos;
    private Vector3 debugLastCastDir;

    private Vector3 castPostion;
    private Vector3 castDirection;

    public Vector3 CastPosition => castPostion;//스킬 시전 위치 저장용
    public Vector3 CastDirection => castDirection;//스킬 시전 방향 저장용
    public Vector3 Position => transform.position;
    public event Action OnSkillEnd;

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetInvincible(bool active)
    {
        onInvincibleChanged?.Invoke(active);
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
        if (targetProvider != null)
        {
            Transform target = targetProvider.GetTarget();
            if (target != null) return target.position;
        }
        return transform.position + transform.forward;
    }
    public Vector3 GetTargetDirection()
    {
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
        if (isCasting) return;
        else
        {
            skillDataContext= dataContext;
        }
       
        if (currentSkillRoutine != null)
            StopCoroutine(currentSkillRoutine);
        CacheCastState();
        currentSkillRoutine = StartCoroutine(SkillSequence(skillDataContext, extraDelay));
    }


    /// <summary>
    ///  시전위치 방향정보 저장, Shadow에 사용
    /// </summary>
    private void CacheCastState()
    {
        castPostion = transform.position;
        castDirection = GetTargetDirection();
    }
    private IEnumerator SkillSequence(SkillDataContext dataContext, float extraDelay = 0)
    {

        isCasting = true;
        SkillData data = dataContext.skillData;
        debugLastSkillData = data;
        if(extraDelay > 0)
        {
            yield return CoroutineManager.waitForSeconds(extraDelay);
        }

        yield return SkillSequenceMove(data);

        yield return SkillSequenceExecute(dataContext);

        OnSkillEnd?.Invoke();

        isCasting = false;
    }

    /// <summary>
    /// 모듈 1 시퀀스
    /// </summary>
    private IEnumerator SkillSequenceMove(SkillData data)
    {
        Vector3 targetPosition = GetTargetPosition();
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
        Vector3 castDirection = GetTargetDirection();
        Vector3 executePivot = transform.position;

        if (delay > 0)
        {
            yield return CoroutineManager.waitForSeconds(delay);
        }
        var m3Strategy = SkillStrategyContainer.GetExecute(m3Type);

        //나중에 프리팹을 넘기는게아니라 데이터 테이블에서 프리팹을 가져오도록 바꿀예정
        GameObject prefab = null;
        if (m3Strategy is ExecuteProjectile)
            prefab = projectilePrefab.gameObject;
        else if (m3Strategy is ExecuteDeploy)
            prefab = deployPrefab.gameObject;

        yield return m3Strategy.Execute(this, this, dataContext, executePivot, castDirection, targetLayer, prefab);
    }
    public void StopSkill()
    {
        if (currentSkillRoutine != null) StopCoroutine(currentSkillRoutine);

        isCasting = false;
        OnSkillEnd?.Invoke();
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

        if (m4Strategy is ISkillCastAddon castAddon)
        {
            castAddon.OnCast(this,this,statProvider,targetProvider, skillDataContext, shadowPrepab);
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].TryGetComponent<IDamageable>(out var target))
            {
                target.TakeDamage(statProvider.GetAttack());

                if (m4Strategy is ISkillHitAddon hitAddon)
                {
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
            if (effect != null) controller.ApplyStatusEffect(effect);
            return;
        }

        if (activeData.applyType == ApplyType.strikeLocation)
        {
            SpawnFireZone(activeData, hitPos);
        }
        else
        {
            var effect = StatusEffectFactory.Create(activeData);
            if (effect != null) controller.ApplyStatusEffect(effect);
        }
    }

    private void SpawnFireZone(SkillModule5Table data, Vector3 position)
    {
        var gameObject = Instantiate(fireZonePrefab, position, transform.rotation);
        if (gameObject.TryGetComponent<FireZone>(out var fireZone))
            fireZone.Init(data, 3.0f, targetLayer);
    }
}
