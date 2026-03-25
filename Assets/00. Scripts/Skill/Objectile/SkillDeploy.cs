using UnityEngine;
using System.Collections;

public class SkillDeploy : SkillObjectileBase
{
    public override void Initialize(ISkillHitHandler owner, SkillDataContext dataContext, LayerMask layer)
    {
        base.Initialize(owner, dataContext, layer);
        debugLastCastPos = dataContext.skillData.m3Data.m3Distance * transform.forward;
        StartCoroutine(RoutineTick());
    }


    private IEnumerator RoutineTick()
    {
        float timer = 0f;
        float interval = data.m3Data.m3TickInterval;

        while (timer < data.m3Data.m3Duration)
        {
            var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
            PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(dataContext?.skillData.m3Data.m3VFX, transform, true, true, rotation: transform.rotation));
            int count = m2.Detect(transform.position, transform.forward, data.m2Data, this, targetLayer);
            if (count > 0)
            {
                owner.HandleSkillHit(count, dataContext, hitBuffer);
            }
            yield return CoroutineManager.waitForSeconds(interval);
            timer += interval;
        }
        ObjectPoolManager.Return(gameObject);
    }

}