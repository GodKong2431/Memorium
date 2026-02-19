using UnityEngine;
using System.Collections;

public class ExecuteDeploy : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer, GameObject prefab)
    {

        SkillData data = dataContext.skillData;
        Vector3 deployPos = startPosition + (direction * dataContext.skillData.m3Data.m3Distance);

        GameObject go = Object.Instantiate(prefab, deployPos, Quaternion.identity);

        if (go.TryGetComponent<SkillObjectileBase>(out var deployer))
        {
            deployer.Initialize(owner, dataContext, targetLayer);
        }

        yield break;
    }
}