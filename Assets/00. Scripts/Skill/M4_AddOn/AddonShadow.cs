using UnityEngine;

public class AddonShadow : ISkillCastAddon
{
    public void OnCast(ISkillHitHandler caster, SkillDataContext dataContext, GameObject prefab)
    {

        Transform casterTransform = caster.transform;

        Vector3 spawnPos = casterTransform.position;
        GameObject cloneObj = Object.Instantiate(prefab, spawnPos, casterTransform.rotation); 
        

        if (cloneObj.TryGetComponent<SkillCaster>(out var cloneCaster))
        {
            cloneCaster.CastSkill(dataContext, dataContext.m4Data.m4Delay);
        }

        Object.Destroy(cloneObj, dataContext.m4Data.m4Delay + 5.0f);
    }
}
