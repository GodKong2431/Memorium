using UnityEngine;
using System.Collections;

public class SkillAura : SkillObjectileBase
{
    public override void Initialize(ISkillHitHandler _owner, SkillDataContext _skillDataContext, LayerMask layer)
    {
        base.Initialize(_owner, _skillDataContext, layer);
        owner.SetChanneling(true);
        StartCoroutine(AuraRoutine());
    }

    private IEnumerator AuraRoutine()
    {
        float timer = 0f;
        float duration = data.m3Data.m3Duration;
        float interval = data.m3Data.m3TickInterval;


        var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);

        while (timer < duration)
        {

            Vector3 targetPos = owner.transform.position + (owner.transform.forward * data.m3Data.m3Distance);
            int count = m2.Detect(targetPos, transform.forward, data.m2Data, this, targetLayer);

            if (count > 0)
            {
                owner.HandleSkillHit(count, skillDataContext, hitBuffer);
            }

            yield return CoroutineManager.waitForSeconds(interval);
            timer += interval;
        }
        owner.SetChanneling(false);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (owner != null)
        {
            owner.SetChanneling(false);
        }
    }
}