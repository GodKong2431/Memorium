using UnityEngine;
using System.Collections;

public class SkillAura : SkillObjectileBase
{
    public override void Initialize(ISkillHitHandler _owner, SkillDataContext dataContext, LayerMask layer)
    {
        base.Initialize(_owner, dataContext, layer);
        owner.SetChanneling(true);
        StartCoroutine(AuraRoutine());
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(dataContext?.skillData.m3Data.m3VFX,transform,true,true));
    }

    private IEnumerator AuraRoutine()
    {
        float timer = 0f;
        float duration = data.m3Data.m3Duration;
        float interval = data.m3Data.m3TickInterval;


        var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);

        while (timer < duration)
        {
            if (IsOwnerDestroyed())
            {
                ReturnPool();
                yield break;
            }

            Vector3 targetPos = owner.transform.position + (owner.transform.forward * data.m3Data.m3Distance);
            int count = m2.Detect(targetPos, transform.forward, data.m2Data, this, targetLayer);

            if (count > 0)
            {
                owner.HandleSkillHit(count, dataContext, hitBuffer);
            }

            yield return CoroutineManager.waitForSeconds(interval);
            timer += interval;
        }
        ReturnPool();
    }

    private void ReturnPool()
    {
        if (IsOwnerDestroyed()) return;
        owner?.SetChanneling(false);
        ObjectPoolManager.Return(gameObject);
    }
    private void OnDisable()
    {
        if (IsOwnerDestroyed()) return;
        owner?.SetChanneling(false);
    }
}