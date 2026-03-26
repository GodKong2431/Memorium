using UnityEngine;
public class AddonImpact : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler owner, Transform target, SkillDataContext data, LayerMask targetLayer, GameObject prefab = null)
    {
        float radius = data.m4Data.m4Distance;
        if (radius <= 0f) return;
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(data.m4Data.m4VFX, target.transform, true));
        if (owner is ISkillDetectable provider)
        {
            Collider[] addonBuffer = provider.GetAddonBuffer();
            float cylinderHalfHeight = SkillConstants.DETECT_HEIGHT;
            Vector3 center = target.transform.position;
            Vector3 pointBottom = center - (Vector3.up * cylinderHalfHeight);
            Vector3 pointTop = center + (Vector3.up * cylinderHalfHeight);

            int count = Physics.OverlapCapsuleNonAlloc(pointBottom, pointTop, radius, addonBuffer, targetLayer);
            if (count > 0)
            {
                owner.HandleAddonHit(count, data, addonBuffer);
            }
        }
    }
}
