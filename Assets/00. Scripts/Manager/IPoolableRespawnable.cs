/// <summary>
/// 풀에서 재스폰될 때 리셋 로직을 구현하는 인터페이스.
/// </summary>
public interface IPoolableRespawnable
{
    void OnSpawnFromPool();
}
