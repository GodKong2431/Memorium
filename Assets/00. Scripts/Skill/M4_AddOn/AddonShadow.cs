using UnityEngine;

public class AddonShadow : ISkillCastAddon
{
    public void OnCast(ISkillHitHandler handler,ISkillCasterMovement caster, 
        ISkillStatProvider stat, ISkillTargetProvider target, SkillDataContext dataContext)
    {
        var originalCaster = caster as SkillCaster;
        if (originalCaster == null) return;
        int m5AId = dataContext.m5DataA?.ID ?? -1;
        int m5BId = dataContext.m5DataB?.ID ?? -1;
        originalCaster.ResetContext(dataContext.skillData.skillTable.ID, -1, m5AId, m5BId);
        Vector3 spawnPos = caster.CastPosition;
        Quaternion spawnRot = Quaternion.LookRotation(caster.CastDirection);
        var cloneObj = PoolAddressableManager.Instance.GetPooledObject("Assets/02. Prefabs/SKill/Shadow/Shadow.prefab", spawnPos, spawnRot);
        if (cloneObj == null)
            return;
        if (cloneObj.TryGetComponent<Shadow>(out var shadow))
        {
            // 원본의 캐싱된 타겟 위치와 시전 방향을 넘겨줌
            shadow.Init(stat, target);
            shadow.Cast(dataContext, SkillConstants.SHADOW_CAST_DELAY, caster.CastTargetPosition, caster.CastDirection);
        }
    }
}