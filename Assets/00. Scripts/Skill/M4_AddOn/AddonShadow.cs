using UnityEngine;

public class AddonShadow : ISkillCastAddon
{
    public void OnCast(ISkillHitHandler handler,ISkillCasterMovement caster, ISkillStatProvider stat, ISkillTargetProvider target, SkillDataContext dataContext, GameObject prefab)
    {

        Vector3 spawnPos = caster.CastPosition;
        Quaternion spawnRot = Quaternion.LookRotation(caster.CastDirection);
        GameObject cloneObj = Object.Instantiate(prefab, spawnPos, spawnRot);

        // 겁나 하드 코딩같은데 뭐......... 잘안떠오르네이거 흠

        if (cloneObj.TryGetComponent<SkillCaster>(out var skillCaster))
        {
            skillCaster.Init(stat, target);
        }

        if (cloneObj.TryGetComponent<Shadow>(out var shadow))
        {
            shadow.Cast(dataContext, SkillConstants.SHADOW_CAST_DELAY);
        }
    }
}