public class CurrencyUIController : UIControllerBase<CurrencyUIView>
{
    // 재화 변경 이벤트를 구독한다.
    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
    }

    // 구독했던 재화 변경 이벤트를 해제한다.
    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
    }

    // 현재 재화 값을 읽어 View에 반영한다.
    protected override void RefreshView()
    {
        if (view == null)
            return;

        view.SetAmount(GetAmount(view.TargetCurrency));
    }

    // 이 View가 표시하는 재화가 바뀐 경우에만 UI를 갱신한다.
    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (view == null || type != view.TargetCurrency)
            return;

        view.SetAmount(amount);
    }

    private static BigDouble GetAmount(CurrencyType type)
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        return currencyModule != null ? currencyModule.GetAmount(type) : BigDouble.Zero;
    }
}
