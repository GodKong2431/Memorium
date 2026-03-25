using TMPro;
using UnityEngine;

public class CurrencyUIController : UIControllerBase
{
    [Header("Currency Binding")]
    [SerializeField] private CurrencyType targetCurrency = CurrencyType.Gold;
    [SerializeField] private TextMeshProUGUI amountText;

    protected override void Initialize()
    {
        RenderAmount(GetAmount(targetCurrency));
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
        RenderAmount(GetAmount(targetCurrency));
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != targetCurrency)
            return;

        RenderAmount(amount);
    }

    private void RenderAmount(BigDouble amount)
    {
        if (amountText != null)
            amountText.text = amount.ToString();
    }

    private static BigDouble GetAmount(CurrencyType type)
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        return currencyModule != null ? currencyModule.GetAmount(type) : BigDouble.Zero;
    }
}
