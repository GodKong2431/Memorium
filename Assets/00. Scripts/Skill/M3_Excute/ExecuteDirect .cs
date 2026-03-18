using UnityEngine;
using System.Collections;
public class ExecuteDirect : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer)
    {
        SkillData data = dataContext.skillData;
        Vector3 hitCenter = startPosition + (direction * data.m3Data.m3Distance);
        var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);

        int count = m2.Detect(hitCenter, direction, data.m2Data, bufferProvider, targetLayer);
        PoolableParticleManager.Instance.SpawnParticle(new ParticleSpawnContext(dataContext?.skillData.m3Data.m3VFX, targetPosition:hitCenter,rotation: owner.transform.rotation));
        if (count > 0)
        {
            owner.HandleSkillHit(count, dataContext, bufferProvider.GetBuffer());
        }
        yield break;
    }
}