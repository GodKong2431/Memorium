using TMPro;
using UnityEngine;

public class CurrencyUIController : UIControllerBase
{
    [Header("Currency Binding")]
    [SerializeField] private CurrencyType targetCurrency = CurrencyType.Gold;
    [SerializeField] private TextMeshProUGUI amountText;

    private CurrencyUIView currencyView;

    protected override void Initialize()
    {
        currencyView = new CurrencyUIView(amountText);
    }

    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
    }

    protected override void RefreshView()
    {
        if (currencyView == null)
            return;

        currencyView.SetAmount(GetAmount(targetCurrency));
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != targetCurrency || currencyView == null)
            return;

        currencyView.SetAmount(amount);
    }

    private static BigDouble GetAmount(CurrencyType type)
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        return currencyModule != null ? currencyModule.GetAmount(type) : BigDouble.Zero;
    }
}
