public class CurrencyUIController : UIControllerBase<CurrencyUIView>
{
    private CurrencyUIService currencyService; // 재화 조회와 변경 이벤트 연결을 담당하는 전역 서비스.

    // 컨트롤러가 사용할 전역 서비스를 준비한다.
    protected override void Initialize()
    {
        currencyService = CurrencyUIService.Instance;
    }

    // 재화 변경 이벤트를 구독한다.
    protected override void Subscribe()
    {
        currencyService.Bind();
        currencyService.CurrencyChanged += OnCurrencyChanged;
    }

    // 구독했던 재화 변경 이벤트를 해제한다.
    protected override void Unsubscribe()
    {
        currencyService.CurrencyChanged -= OnCurrencyChanged;
        currencyService.Unbind();
    }

    // 현재 재화 값을 읽어 View에 반영한다.
    protected override void RefreshView()
    {
        if (view == null)
            return;

        view.SetAmount(currencyService.GetAmount(view.TargetCurrency));
    }

    // 이 View가 표시하는 재화가 바뀐 경우에만 UI를 갱신한다.
    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (view == null || type != view.TargetCurrency)
            return;

        view.SetAmount(amount);
    }
}
