using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;
public struct ParticleSpawnContext
{
    public string key;
    public Transform target;
    public bool follow;
    public bool autoReturn;
    public float scale;
    public Quaternion rotation;
    public Action<PoolableParticle> onSpawned;

    public ParticleSpawnContext(string key, Transform target,bool follow = false,bool autoReturn = true,
                                float scale = 1f,Quaternion? rotation = null,Action<PoolableParticle> onSpawned = null)
    {
        this.key = key;
        this.target = target;
        this.follow = follow;
        this.autoReturn = autoReturn;
        this.scale = scale;
        this.rotation = rotation ?? Quaternion.identity;
        this.onSpawned = onSpawned;
    }
}

public class PoolableParticleManager : Singleton<PoolableParticleManager>
{
    private Dictionary<string, GameObject> prefabCache = new();

    public PoolableParticle SpawnParticle(ParticleSpawnContext ctx)
    {
        if (string.IsNullOrEmpty(ctx.key)) return null;

        if (prefabCache.TryGetValue(ctx.key, out var cached))
        {
            var prefab = SpawnFromPrefab(cached, ctx);
            ctx.onSpawned?.Invoke(prefab);
            return prefab;
        }

        Addressables.LoadAssetAsync<GameObject>(ctx.key).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded) return;
            prefabCache[ctx.key] = handle.Result;
            var prefab = SpawnFromPrefab(handle.Result, ctx);
            ctx.onSpawned?.Invoke(prefab);
        };

        return null;
    }

    private PoolableParticle SpawnFromPrefab(GameObject prefab, ParticleSpawnContext ctx)
    {
        if (prefab == null) return null;

        var pos = ctx.target != null ? ctx.target.position : Vector3.zero;
        var obj = ObjectPoolManager.Get(prefab, pos, ctx.rotation);
        if (obj.TryGetComponent<PoolableParticle>(out var particle))
        {
            if (ctx.follow && ctx.target != null) particle.SetFollow(ctx.target);
            particle.SetAutoReturn(ctx.autoReturn);
            if (ctx.scale != 1f) obj.transform.localScale = Vector3.one * ctx.scale;
            return particle;
        }

        return null;
    }
}