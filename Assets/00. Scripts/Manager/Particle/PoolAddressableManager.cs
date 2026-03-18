using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
public class PoolAddressableManager : Singleton<PoolAddressableManager>
{
    private Dictionary<string, GameObject> prefabCache = new();

    public void Preload(string key)
    {
        if (!IsValidKey(key) || prefabCache.ContainsKey(key)) return;

        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
                prefabCache[key] = handle.Result;
        };
    }


    /// <summary>
    /// Preload 했을 경우
    /// </summary>
    public GameObject GetPooledObject(string key, Vector3 position, Quaternion rotation)
    {
        var prefab = GetCachedObject(key);
        if (prefab == null) return null;
        return ObjectPoolManager.Get(prefab, position, rotation);
    }

    /// <summary>
    /// Preload 안 했을경우 콜백과 함께 요청
    /// </summary>
    public void GetPooledObject(string key, Vector3 position, Quaternion rotation,
                                 System.Action<GameObject> onLoaded)
    {
        var prefab = GetCachedObject(key);
        if (prefab != null)
        {
            onLoaded?.Invoke(ObjectPoolManager.Get(prefab, position, rotation));
            return;
        }

        Addressables.LoadAssetAsync<GameObject>(key).Completed += handle =>
        {
            if (handle.Status != AsyncOperationStatus.Succeeded) return;
            prefabCache[key] = handle.Result;
            onLoaded?.Invoke(ObjectPoolManager.Get(handle.Result, position, rotation));
        };
    }

    private GameObject GetCachedObject(string key)
    {
        if (!IsValidKey(key) && prefabCache.TryGetValue(key, out var prefab))
            return prefab;
        return null;
    }
    private bool IsValidKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key != "0";
    }
}