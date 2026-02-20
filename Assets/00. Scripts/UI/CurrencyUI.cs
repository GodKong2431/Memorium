using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [Header("재화 선택")]
    public CurrencyType targetCurrency;

    [Header("UI")]
    public TextMeshProUGUI textAmount;

    private void Start()
    {
        GameEventManager.OnCurrencyChanged += UpdateUI;
    }

    private void OnDestroy()
    {
        GameEventManager.OnCurrencyChanged -= UpdateUI;
    }

    private void UpdateUI(CurrencyType type, BigDouble currentAmount)
    {
        if (this.targetCurrency == type && textAmount != null)
        {
            textAmount.text = currentAmount.ToString();
        }
    }
}