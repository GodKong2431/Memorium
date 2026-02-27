using System;

public abstract class UIServiceBase<TService> where TService : UIServiceBase<TService>
{
    private static readonly Lazy<TService> lazyInstance = new Lazy<TService>(CreateInstance); // 서비스별 전역 인스턴스.
    private int bindCount; // 현재 서비스 구독자 수.

    public static TService Instance => lazyInstance.Value;

    public void Bind()
    {
        bindCount++;
        if (bindCount == 1)
            OnBind();
    }

    public void Unbind()
    {
        if (bindCount <= 0)
            return;

        bindCount--;
        if (bindCount == 0)
            OnUnbind();
    }

    protected abstract void OnBind();
    protected abstract void OnUnbind();

    private static TService CreateInstance()
    {
        return (TService)Activator.CreateInstance(typeof(TService), true);
    }
}
