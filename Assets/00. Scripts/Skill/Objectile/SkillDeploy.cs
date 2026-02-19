using UnityEngine;
using System.Collections;

public class SkillDeploy : SkillObjectileBase
{
    public override void Initialize(ISkillHitHandler owner, SkillDataContext skillDataContext, LayerMask layer)
    {
        base.Initialize(owner, skillDataContext, layer);
        StartCoroutine(RoutineTick());
    }


    private IEnumerator RoutineTick()
    {
        float timer = 0f;
        float interval = data.m3Data.m3TickInterval;

        while (timer < data.m3Data.m3Duration)
        {
            var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
            int count = m2.Detect(transform.position, transform.forward, data.m2Data, this, targetLayer);
            if (count > 0)
            {
                owner.HandleSkillHit(count, skillDataContext, hitBuffer);
            }
            yield return CoroutineManager.waitForSeconds(interval);
            timer += interval;
        }
        Destroy(gameObject);
    }

}