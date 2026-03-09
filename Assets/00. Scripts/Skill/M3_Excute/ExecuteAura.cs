using UnityEngine;
using System.Collections;

public class ExecuteAura : ISkillExecuteStrategy
{
    public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer, GameObject prefab)
    {
        GameObject go = Object.Instantiate(prefab, owner.transform.position, owner.transform.rotation, owner.transform);

        if (go.TryGetComponent<SkillObjectileBase>(out var aura))
        {
            aura.Initialize(owner, dataContext, targetLayer);
        }

        yield break;
    }
}