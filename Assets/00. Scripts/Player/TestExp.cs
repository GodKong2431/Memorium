using UnityEngine;
using UnityEngine.UI;

public class TestExp : MonoBehaviour
{
    [SerializeField] Button button;

    [SerializeField] private CurrencyType currencyType;

    [SerializeField] Button buttone;

    [SerializeField] private PlayerStatType playerStatType;

    [SerializeField] private BigDouble amount;
    [SerializeField] private BigDouble currentAmount;

    private void Awake()
    {
        button.onClick.AddListener(() => CurrencyManager.Instance.AddCurrency(currencyType, amount));
        button.onClick.AddListener(() => currentAmount = CurrencyManager.Instance.GetAmount(currencyType));

        buttone.onClick.AddListener(() => CharacterStatManager.Instance.FinalStat(playerStatType));
    }
}
