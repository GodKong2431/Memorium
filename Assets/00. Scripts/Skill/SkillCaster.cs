using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 원래 스킬 세개가있는구조 
/// 나중에 얘는 정말 스킬실행만시키고 쿨타임이나 마나계산같은건 스킬캐스터를 가지는 클래스를 만들어서 플레이어에 컴포넌트로 붙이려고함
/// 이유는 분신이나 아니면 마나없거나 아니면 몬스터가 이 스킬캐스터를 가질수있더라도 각자 자신의 로직에따라서 실행시키기 위해서임
/// </summary>
public class SkillCaster : MonoBehaviour, ISkillMovementTarget, ISkillHitHandler, ISkillDetectable
{
    private ISkillStatProvider statProvider;
    private ISkillTargetProvider targetProvider;
    [Header("레이어")]
    [SerializeField] private LayerMask targetLayer; 

    [Header("테스트")]
    [SerializeField] private SkillDataContext testskillDataContext;
    [SerializeField] SkillProjectile projectilePrefab;
    [SerializeField] SkillDeploy deployPrefab;
    [SerializeField] GameObject shadowPrepab;
    [SerializeField] private bool isTestContextOn = false;

    private SkillDataContext skillDataContext;

    private bool isCasting = false;
    public bool IsCasting => isCasting;
    private Coroutine currentSkillRoutine;

    private Collider[] hitBuffer = new Collider[20];//타격 대상 버퍼, nonalloc 저장용도

    // 기즈모 디버그용, 다른 시각적 방식으로 바꾸는게 좋을것 같음.
    private SkillData debugLastSkillData;
    private Vector3 debugLastCastPos;
    private Vector3 debugLastCastDir;

    public Vector3 Position => transform.position;

    public void SetPosition(Vector3 position)
    {
        transform.position = position;
    }

    public void SetInvincible(bool active)
    {
        Debug.Log(active ? "무적" : "무적 해제");
    }

    public void PlayAnim(string key)
    {
        Debug.Log($"애니메이션 재생: {key}");
    }

    public void Init(ISkillStatProvider stat, ISkillTargetProvider target)
    {
        statProvider = stat;
        targetProvider = target;
    }

    public void CastSkill(SkillDataContext dataContext, float extraDelay = 0)
    {
        if (isCasting) return;
        if (isTestContextOn)
        {
            skillDataContext = testskillDataContext;
        }
        else
        {
            skillDataContext= dataContext;
        }

        Enemy_PlayerMove tmp = GetComponent<Enemy_PlayerMove>(); //분신이랑 플레이어 구분용, 임시로 겟컴포넌트로 구분해놨고 , id가 아니라 m4컨텍스트를 비워서 주는쪽이 좋을듯
        if (tmp!=null&&skillDataContext.m4Data != null)
        {
            var m4Strategy = SkillStrategyContainer.GetAddon(skillDataContext.m4Data.m4Type);
            if (m4Strategy is ISkillCastAddon castAddon)
            {
                castAddon.OnCast(this, skillDataContext, shadowPrepab); 
            }

        }
        if (currentSkillRoutine != null)
            StopCoroutine(currentSkillRoutine);
        currentSkillRoutine = StartCoroutine(SkillSequence(skillDataContext, extraDelay));
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


        Transform target = targetProvider.GetTarget();
        Vector3 castDirection = (target.position - transform.position);
        castDirection.y = 0;
        castDirection = castDirection.normalized;
        Vector3 ExecutePivot = transform.position;

        if (data.m3Data.m3Delay > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m3Data.m3Delay);
        }
        var m3Strategy = SkillStrategyContainer.GetExecute(data.m3Data.m3Type);

        //나중에 프리팹을 넘기는게아니라 데이터 테이블에서 프리팹을 가져오도록 바꿀예정
        GameObject prefab = null;
        if (m3Strategy is ExecuteProjectile)
            prefab = projectilePrefab.gameObject;
        else if (m3Strategy is ExecuteDeploy)
            prefab = deployPrefab.gameObject; 

        yield return m3Strategy.Execute(this, this, dataContext, ExecutePivot, castDirection, targetLayer, prefab);

        isCasting = false;
    }

    /// <summary>
    /// 모듈 1 시퀀스
    /// </summary>
    private IEnumerator SkillSequenceMove(SkillData data)
    {
        Transform target = targetProvider.GetTarget();
        Vector3 castDirection = (target.position - transform.position);
        castDirection.y = 0;
        castDirection = castDirection.normalized;

        if (data.m1Data.m1Delay > 0)
        {
            yield return CoroutineManager.waitForSeconds(data.m1Data.m1Delay);
        }
        var m1Strategy = SkillStrategyContainer.GetMovement(data.m1Data.m1Type);

        yield return m1Strategy.SkillMove(this, castDirection, data.m1Data);
    }
    public void StopSkill()
    {
        if (currentSkillRoutine != null) StopCoroutine(currentSkillRoutine);

        isCasting = false;

        Debug.LogWarning("스킬 시전 중단");
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
        ISkillAddonStrategy m4Strategy = null;
        if (applyAddon && data.m4Data != null)
        {
            m4Strategy = SkillStrategyContainer.GetAddon(data.m4Data.m4Type);
        }

        for (int i = 0; i < hitCount; i++)
        {
            if (hitBuffer[i].TryGetComponent<EnemyStateMachine>(out var target))
            {
                target.TakeDamage(skillDataContext.skillData.skillTable.skillDamage);

                if (m4Strategy is ISkillHitAddon hitAddon)
                {
                    hitAddon.OnHit(this, target.gameObject, data, targetLayer);
                }
            }
        }
    }
}