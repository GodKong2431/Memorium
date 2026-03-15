using UnityEngine;

public class AddonPull : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler caster, Transform target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null)
    {
        Transform casterTransform = caster.transform;

        Vector3 direction = (casterTransform.position - target.transform.position);
        direction.y = 0; 
        direction = direction.normalized;

        float distance = dataContext.m4Data.m4Distance;
        float duration = dataContext.m4Data.m4Duration;

        if (target.TryGetComponent<IKnockbackable>(out var damageable))
        {
            damageable.ApplyKnockback(direction, distance, duration);
        }
    }
}