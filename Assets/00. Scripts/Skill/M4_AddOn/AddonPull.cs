using UnityEngine;

public class AddonPull : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler caster, GameObject target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null)
    {
        Transform casterTransform = caster.transform;

        Vector3 Direction = (casterTransform.position - target.transform.position);
        Direction.y = 0; 
        Direction = Direction.normalized;

        float force = dataContext.m4Data.m4Distance;

        target.transform.Translate(Direction * force, Space.World);
    }
}