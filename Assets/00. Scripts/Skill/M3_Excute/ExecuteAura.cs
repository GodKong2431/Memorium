
using UnityEngine;
using System.Collections;

//객채 생성하는걸로 바꿔야함.
public class ExecuteAura : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer, GameObject prefab =null)
    {
        SkillData data = dataContext.skillData;
        float duration = data.m3Data.m3Duration;
        float interval = data.m3Data.m3TickInterval;
        float timer = 0f;

        var m2 = SkillStrategyContainer.GetDetect(data.m2Data.m2Type);
        Transform targetTransform = owner.transform;

        while (timer < duration)
        {
            if (targetTransform == null) yield break;

            Vector3 currentPos = targetTransform.position + (targetTransform.forward * data.m3Data.m3Distance);

            Debug.DrawRay(currentPos, Vector3.up * 2f, Color.green, 0.5f);

            int count = m2.Detect(currentPos, targetTransform.forward, data.m2Data, bufferProvider, targetLayer);

            if (count > 0)
            {
                owner.HandleSkillHit(count, dataContext, bufferProvider.GetBuffer());
            }
            yield return CoroutineManager.waitForSeconds(interval); 
            timer += interval;
        }
    }
}