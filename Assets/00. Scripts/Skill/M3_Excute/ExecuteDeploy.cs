using UnityEngine;
using System.Collections;

public class ExecuteDeploy : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer)
    {
        SkillData data = dataContext.skillData;
        Vector3 targetPos = startPosition + (direction * dataContext.skillData.m3Data.m3Distance);


        var obj = PoolAddressableManager.Instance.GetPooledObject("Assets/02. Prefabs/SKill/Deploy/Deploy.prefab", targetPos, owner.transform.localRotation);

        if (obj == null) yield break;
        if (obj.TryGetComponent<SkillObjectileBase>(out var deployer))
        {
            deployer.Initialize(owner, dataContext, targetLayer);
        }

        yield break;
    }
}