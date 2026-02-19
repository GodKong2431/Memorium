using UnityEngine;
using System.Collections;
public class ExecuteProjectile : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer, GameObject prefab)
    {
        Vector3 spawnPos = startPosition + (direction * dataContext.skillData.m3Data.m3Distance);

        GameObject go = Object.Instantiate(prefab, spawnPos, Quaternion.LookRotation(direction));

        if (go.TryGetComponent<SkillObjectileBase>(out var projectile))
        {
            projectile.Initialize(owner, dataContext, targetLayer);
        }
        go.SetActive(true);
        yield break;
    }
}