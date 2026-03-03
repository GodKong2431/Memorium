using System;

public sealed class CurrencyUIService : UIServiceBase<CurrencyUIService>
{
    public event Action<CurrencyType, BigDouble> CurrencyChanged; // 재화가 바뀌었을 때 UI에 전달하는 이벤트.

    private CurrencyUIService()
    {
    }

    // 현재 재화 수량을 조회한다.
    public BigDouble GetAmount(CurrencyType type)
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        var currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        return currencyModule != null ? currencyModule.GetAmount(type) : BigDouble.Zero;
    }

    // CurrencyInventoryModule의 재화 변경 이벤트를 구독한다.
    protected override void OnBind()
    {
        var currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currencyModule == null)
            return;

        currencyModule.OnCurrencyChanged += HandleCurrencyChanged;
    }

    // CurrencyInventoryModule의 재화 변경 이벤트 구독을 해제한다.
    protected override void OnUnbind()
    {
        var currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currencyModule == null)
            return;

        currencyModule.OnCurrencyChanged -= HandleCurrencyChanged;
    }

    // 매니저에서 받은 이벤트를 UI용 이벤트로 다시 전달한다.
    private void HandleCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        CurrencyChanged?.Invoke(type, amount);
    }
}
