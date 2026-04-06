using UnityEngine;

[RequireComponent(typeof(SkillCaster))]
public class Shadow : MonoBehaviour{
    SkillCaster caster;
    private void Awake()
    {
        caster = GetComponent<SkillCaster>();
    }
    private void OnEnable()
    {
        if (caster == null)
            caster = GetComponent<SkillCaster>();
        caster.OnSkillEnd += SelfDestroy;

    }
    private void OnDisable()
    {
        caster.OnSkillEnd -= SelfDestroy;
    }
    public void Init(ISkillStatProvider stat, ISkillTargetProvider target)
    {
        caster.Init(stat, target);
    }
    public void Cast(SkillDataContext originalContext, float delay,
                     Vector3 originTargetPos, Vector3 originDir)
    {
        caster.SetShadowData(originTargetPos, originDir);
        caster.CastSkill(originalContext, delay, false);
    }
    private void SelfDestroy()
    {
        ObjectPoolManager.Return(gameObject);
    }
}
