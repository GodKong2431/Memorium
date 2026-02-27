using System;

public abstract class UISharedServiceBase<TService> : UIServiceBase where TService : UISharedServiceBase<TService>
{
    private static readonly Lazy<TService> lazyInstance = new Lazy<TService>(CreateInstance); // 서비스별 전역 인스턴스를 지연 생성한다.

    public static TService Instance => lazyInstance.Value;

    // 파생 서비스의 비공개 생성자를 사용해 전역 인스턴스를 만든다.
    private static TService CreateInstance()
    {
        return (TService)Activator.CreateInstance(typeof(TService), true);
    }
}
