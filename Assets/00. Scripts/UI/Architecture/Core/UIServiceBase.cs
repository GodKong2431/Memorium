public abstract class UIServiceBase
{
    private int bindCount; // 현재 이 서비스를 사용 중인 구독자 수.

    // 첫 번째 구독자가 붙을 때만 실제 이벤트 연결을 수행한다.
    public void Bind()
    {
        bindCount++;

        if (bindCount != 1)
            return;

        OnBind();
    }

    // 마지막 구독자가 해제될 때만 실제 이벤트 연결을 해제한다.
    public void Unbind()
    {
        if (bindCount <= 0)
            return;

        bindCount--;

        if (bindCount != 0)
            return;

        OnUnbind();
    }

    // 실제 이벤트 연결 로직은 파생 서비스에서 구현한다.
    protected abstract void OnBind();

    // 실제 이벤트 해제 로직은 파생 서비스에서 구현한다.
    protected abstract void OnUnbind();
}
