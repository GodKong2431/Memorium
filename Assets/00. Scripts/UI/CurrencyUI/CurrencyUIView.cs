using TMPro;

/// <summary>
/// Plain view for simple currency amount rendering.
/// </summary>
public sealed class CurrencyUIView
{
    private readonly TextMeshProUGUI amountText;

    public CurrencyUIView(TextMeshProUGUI amountText)
    {
        this.amountText = amountText;
    }

    public void SetAmount(BigDouble amount)
    {
        if (amountText == null)
            return;

        amountText.text = amount.ToString();
    }
}
