
using System;
using UnityEngine;
public struct ParticleSpawnContext
{
    public string key;
    public Transform target;
    public bool follow;
    public bool autoReturn;
    public float scale;
    public Quaternion rotation;
    public Action<PoolableParticle> onSpawned;
    public Vector3 targetPosition;

    public ParticleSpawnContext(string key, Transform target = null, bool follow = false, bool autoReturn = true,
                                float scale = 1f, Quaternion? rotation = null, Action<PoolableParticle> onSpawned = null, Vector3? targetPosition = null)
    {
        this.key = key;
        this.target = target;
        this.follow = follow;
        this.autoReturn = autoReturn;
        this.scale = scale;
        this.rotation = rotation ?? Quaternion.identity;
        this.onSpawned = onSpawned;
        this.targetPosition = targetPosition ?? Vector3.zero; 
    }
}

public class PoolableParticleManager : Singleton<PoolableParticleManager>
{
    public void Preload(string key)
    {
        PoolAddressableManager.Instance.Preload(key);
    }
    /// <summary>
    /// 생성된 파티클을 보관해야 할 경우, 
    /// Action<PoolableParticle> onSpawned 통해서 해당 오브젝트를 저장하는 함수를 넘길것. Preload로 미리 로드 해놨다면 안넘겨도되긴함.
    /// </summary>
    public void SpawnParticle(ParticleSpawnContext ctx)
    {
        if (!IsValidKey(ctx.key)) return;

        var pos = ctx.target != null ? ctx.target.position : ctx.targetPosition;
        var obj = PoolAddressableManager.Instance.GetPooledObject(ctx.key, pos, ctx.rotation);// 프리로드해서 매니저에 이미 있었을경우

        if (obj != null)
        {
            SetupParticle(obj, ctx);
            return;
        }

        PoolAddressableManager.Instance.GetPooledObject(ctx.key, pos, ctx.rotation, loadedObj => SetupParticle(loadedObj, ctx)); // 프리로드 안해놔서 없을경우 
    }

    private void SetupParticle(GameObject obj, ParticleSpawnContext ctx)
    {
        if (obj == null) return;

        if (obj.TryGetComponent<PoolableParticle>(out var particle))
        {
            if (ctx.follow && ctx.target != null) particle.SetFollow(ctx.target);
            particle.SetAutoReturn(ctx.autoReturn);
            if (ctx.scale != 1f) obj.transform.localScale = Vector3.one * ctx.scale;
            ctx.onSpawned?.Invoke(particle);
        }
    }
    private bool IsValidKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key != "0";
    }
}