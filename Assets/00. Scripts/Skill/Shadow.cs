using UnityEngine;

[RequireComponent(typeof(SkillCaster))]
public class Shadow : MonoBehaviour
{
    SkillCaster caster;

    private void Awake()
    {
        caster = GetComponent<SkillCaster>();

    }
    private void OnEnable()
    {
        caster.OnSkillEnd += SelfDestroy;
    }
    private void OnDisable()
    {
        caster.OnSkillEnd -= SelfDestroy;
    }

    public void Cast(SkillDataContext originalContext, float delay)
    {
        int skillID = originalContext.skillData.skillTable.ID;
        int m5ID = originalContext.m5Data != null ? originalContext.m5Data.ID : -1;
        caster.CastSkill(caster.ResetContext(skillID, -1, m5ID), delay, false);
    }

    private void SelfDestroy()
    {
        Destroy(gameObject);
    }
}
