using TMPro;
using UnityEngine;

public class CurrencyUIView : UIViewBase
{
    [SerializeField] private CurrencyType targetCurrency = CurrencyType.Gold; // 이 UI가 표시할 재화 종류.
    [SerializeField] private TextMeshProUGUI amountText; // 재화 수량을 출력할 텍스트.

    public CurrencyType TargetCurrency => targetCurrency;

    // 전달받은 재화 수량을 텍스트에 반영한다.
    public void SetAmount(BigDouble amount)
    {
        if (amountText == null)
            return;

        amountText.text = amount.ToString();
    }
}
