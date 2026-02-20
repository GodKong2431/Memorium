using UnityEngine;

/// <summary>
/// 오브젝트 풀에서 관리되는 오브젝트에 부착.
/// 풀에서 꺼낼 때 OnSpawnFromPool, 반환 시 OnReturnToPool 호출.
/// </summary>
public class PoolableObject : MonoBehaviour
{
    private GameObject _prefab;
    private int _prefabId;

    public int PrefabId => _prefabId;

    public void Initialize(GameObject prefab)
    {
        _prefab = prefab;
        _prefabId = prefab != null ? prefab.GetInstanceID() : 0;
    }

    /// <summary>
    /// 풀에서 꺼낼 때 호출. 리셋 로직은 여기서 처리.
    /// </summary>
    public virtual void OnSpawnFromPool()
    {
        var respawnable = GetComponent<IPoolableRespawnable>();
        respawnable?.OnSpawnFromPool();
    }

    /// <summary>
    /// 풀에 반환될 때 호출.
    /// </summary>
    public virtual void OnReturnToPool()
    {
    }

    /// <summary>
    /// 수동으로 풀에 반환.
    /// </summary>
    public void ReturnToPool()
    {
        ObjectPoolManager.Return(gameObject);
    }
}
