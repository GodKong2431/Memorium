using UnityEngine;

public class AddonPush : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler caster, Transform target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null)
    {
        Transform casterTransform = caster.transform;

        Vector3 direction = (target.transform.position - casterTransform.position);
        direction.y = 0;
        direction = direction.normalized;

        float distance = dataContext.m4Data.m4Distance;
        float duration = dataContext.m4Data.m4Duration;

#if UNITY_EDITOR
        Debug.Log("넉백");
#endif
        if (target.TryGetComponent<IKnockbackable>(out var damageable))
        {
#if UNITY_EDITOR
            Debug.Log("타겟넉백");
#endif
            damageable.ApplyKnockback(direction, distance, duration);
        }
    }
}