using UnityEngine;

public class AddonImpact : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler owner, Transform target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null)
    {
        var detectStrategy = SkillStrategyContainer.GetDetect(M2Type.circle);
        if (detectStrategy == null) return;
        
        SkillModule2Table tempM2Data = new SkillModule2Table{m2Type = M2Type.circle,m2S1 = dataContext.m4Data.m4Distance};


        if (owner is ISkillDetectable provider)
        {
            int count = detectStrategy.Detect(target.transform.position, Vector3.zero, tempM2Data, provider, targetLayer);

            if (count > 0)
            {
                owner.HandleAddonHit(count, dataContext, provider.GetBuffer());
            }
        }
    }
}
