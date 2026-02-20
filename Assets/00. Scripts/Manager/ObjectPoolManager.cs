using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 프리팹별 오브젝트 풀 관리.
/// Get 시 풀에 있으면 재사용, 없으면 Instantiate.
/// Return 시 비활성화 후 풀에 반환.
/// </summary>
public static class ObjectPoolManager
{
    /// <summary>true면 Get/Return 시 Debug.Log 출력 (테스트용).</summary>
    public static bool DebugLog { get; set; }

    private static readonly Dictionary<int, Queue<GameObject>> _pools = new Dictionary<int, Queue<GameObject>>();
    private static Transform _poolRoot;

    private static Transform PoolRoot
    {
        get
        {
            if (_poolRoot == null)
            {
                var go = new GameObject("[ObjectPool]");
                Object.DontDestroyOnLoad(go);
                _poolRoot = go.transform;
            }
            return _poolRoot;
        }
    }

    /// <summary>
    /// 풀에서 오브젝트를 가져오거나, 없으면 새로 생성.
    /// </summary>
    public static GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();
        if (!_pools.TryGetValue(key, out var queue))
            _pools[key] = queue = new Queue<GameObject>();

        GameObject obj;
        if (queue.Count > 0)
        {
            obj = queue.Dequeue();
            obj.transform.SetParent(parent ?? PoolRoot);
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);

            var poolable = obj.GetComponent<PoolableObject>();
            poolable?.OnSpawnFromPool();
            if (DebugLog) Debug.Log($"[ObjectPool] Get (풀에서 꺼냄): {prefab.name} → {obj.name}");
        }
        else
        {
            obj = Object.Instantiate(prefab, position, rotation, parent ?? PoolRoot);
            if (DebugLog) Debug.Log($"[ObjectPool] Get (새로 생성): {prefab.name}");
            var poolable = obj.GetComponent<PoolableObject>();
            if (poolable == null)
                poolable = obj.AddComponent<PoolableObject>();
            poolable.Initialize(prefab);
        }

        return obj;
    }

    /// <summary>
    /// 오브젝트를 풀에 반환. PoolableObject가 있어야 함.
    /// </summary>
    public static void Return(GameObject obj)
    {
        if (obj == null) return;

        var poolable = obj.GetComponent<PoolableObject>();
        if (poolable == null)
        {
            Object.Destroy(obj);
            return;
        }

        poolable.OnReturnToPool();
        obj.SetActive(false);
        obj.transform.SetParent(PoolRoot);
        if (DebugLog) Debug.Log($"[ObjectPool] Return: {obj.name} → 풀에 반환");

        int key = poolable.PrefabId;
        if (!_pools.TryGetValue(key, out var queue))
            _pools[key] = queue = new Queue<GameObject>();
        queue.Enqueue(obj);
    }

    /// <summary>
    /// 풀에 오브젝트가 풀링 가능한지 확인.
    /// </summary>
    public static bool IsPooled(GameObject obj)
    {
        return obj != null && obj.GetComponent<PoolableObject>() != null;
    }

#if UNITY_EDITOR
    /// <summary>
    /// [테스트용] 풀 상태 로그. 콘솔에서 호출하거나 에디터 스크립트에서 사용.
    /// </summary>
    public static void LogPoolStatus()
    {
        int totalCount = 0;
        foreach (var kv in _pools)
        {
            int count = kv.Value.Count;
            totalCount += count;
            if (count > 0)
                Debug.Log($"[ObjectPool] PrefabId {kv.Key}: {count}개 대기");
        }
        Debug.Log($"[ObjectPool] 총 대기 오브젝트: {totalCount}개");
    }
#endif
}
