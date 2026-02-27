using UnityEngine;

public class AddonPush : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler caster, Transform target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null)
    {
        Transform casterTransform = caster.transform;

        Vector3 direction = (target.transform.position - casterTransform.position);
        direction.y = 0;
        direction = direction.normalized;

        float force = dataContext.m4Data.m4Distance;

        target.transform.Translate(direction * force, Space.World);
    }
}