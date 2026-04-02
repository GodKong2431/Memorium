/// <summary>
/// 풀에 반환되기 직전 비활성화 전에 호출. 애니메이션·이펙트·런타임 상태 정리용.
/// </summary>
public interface IPoolableReturnable
{
    void OnReturnToPool();
}
