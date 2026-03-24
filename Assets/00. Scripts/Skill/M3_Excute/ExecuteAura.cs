
    using UnityEngine;
    using System.Collections;

    //객채 생성하는걸로 바꿔야함.
    public class ExecuteAura : ISkillExecuteStrategy
    {
        public IEnumerator Execute(ISkillHitHandler owner, ISkillDetectable bufferProvider, SkillDataContext dataContext, Vector3 startPosition, Vector3 direction, LayerMask targetLayer)
        {


            var obj = PoolAddressableManager.Instance.GetPooledObject("Assets/02. Prefabs/SKill/Aura/Aura.prefab", startPosition, rotation: Quaternion.LookRotation(direction));

            if (obj == null) yield break;
            if (obj.TryGetComponent<SkillObjectileBase>(out var aura))
            {
                aura.Initialize(owner, dataContext, targetLayer);
            }

            yield break;
        }
    }