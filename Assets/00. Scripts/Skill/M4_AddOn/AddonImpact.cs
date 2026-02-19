using UnityEngine;

public class AddonImpact : ISkillHitAddon
{
    public void OnHit(ISkillHitHandler owner, GameObject target, SkillDataContext dataContext, LayerMask targetLayer, GameObject prefab = null)
    {
        var detectStrategy = SkillStrategyContainer.GetDetect(M2Type.circle);
        if (detectStrategy == null) return;
        
        SkillModule2Table tempM2Data = new SkillModule2Table{m2Type = M2Type.circle,m2S1 = dataContext.m4Data.m4Distance};

        ISkillDetectable provider = owner as ISkillDetectable;

        int count = detectStrategy.Detect(target.transform.position,Vector3.zero,tempM2Data,provider,targetLayer);

        dataContext.m4Data = null;
        if (count > 0 && provider != null)
        {
            owner.HandleAddonHit(count, dataContext, provider.GetBuffer());
        }

        var targets = provider.GetBuffer();

        for (int i = 0; i < count; i++)
        {
            Debug.Log($"{targets[i].name} 스플래시");
        }
    }
}
