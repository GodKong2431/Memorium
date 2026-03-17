using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class PoolableParticleManager : Singleton<PoolableParticleManager>
{
    private Dictionary<string, GameObject> prefabCache = new();

    public PoolableParticle SpawnParticle(string key, Transform followTarget, bool autoReturn)
    {
        if (string.IsNullOrEmpty(key)) return null;

        if (prefabCache.TryGetValue(key, out var cached))
        {
            return SpawnFromPrefab(cached, followTarget, autoReturn);
        }

        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded) return;
            prefabCache[key] = handle.Result;
        };

        return null;
    }

    private PoolableParticle SpawnFromPrefab(GameObject prefab, Transform followTarget, bool autoReturn)
    {
        if (prefab == null) return null;

        var pos = followTarget != null ? followTarget.position : Vector3.zero;
        var obj = ObjectPoolManager.Get(prefab, pos, Quaternion.identity);
        var particle = obj.GetComponent<PoolableParticle>();

        if (particle != null)
        {
            if (followTarget != null) particle.SetFollow(followTarget);
            particle.SetAutoReturn(autoReturn);
        }

        return particle;
    }
}