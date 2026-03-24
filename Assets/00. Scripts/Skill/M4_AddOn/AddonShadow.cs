using UnityEngine;

public class AddonShadow : ISkillCastAddon
{
    public void OnCast(ISkillHitHandler handler,ISkillCasterMovement caster, ISkillStatProvider stat, ISkillTargetProvider target, SkillDataContext dataContext)
    {

        var originalCaster = caster as SkillCaster;
        if (originalCaster == null) return;

        Vector3 spawnPos = originalCaster.CastPosition;
        Quaternion spawnRot = Quaternion.LookRotation(originalCaster.CastDirection);

        var cloneObj = PoolAddressableManager.Instance.GetPooledObject("Assets/02. Prefabs/SKill/Shadow/Shadow.prefab", spawnPos, spawnRot);
        if (cloneObj == null)
            return;

        if (cloneObj.TryGetComponent<SkillCaster>(out var skillCaster))
        {
            skillCaster.Init(stat, target);
        }

        if (cloneObj.TryGetComponent<Shadow>(out var shadow))
        {
            // 원본의 캐싱된 타겟 위치와 시전 방향을 넘겨줌
            shadow.Cast(dataContext, SkillConstants.SHADOW_CAST_DELAY, originalCaster.CastTargetPosition, originalCaster.CastDirection);
        }
    }
}