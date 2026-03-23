using UnityEngine;
using System.Collections;
public class ExecuteProjectile : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer)
    {
        Vector3 spawnPos = startPosition + (direction * dataContext.skillData.m3Data.m3Distance);
        
        var obj = PoolAddressableManager.Instance.GetPooledObject("Assets/02. Prefabs/SKill/Projectile/bullet.prefab",spawnPos, Quaternion.LookRotation(direction));


        if (obj == null) yield break;
        if (obj.TryGetComponent<SkillObjectileBase>(out var projectile))
        {
            projectile.Initialize(owner, dataContext, targetLayer);
        }
        yield break;
    }
}