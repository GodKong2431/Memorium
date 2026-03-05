using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스탯 한 행 프리팹의 직렬화된 UI 참조를 보관한다.
/// </summary>
public class PlayerStatUpgradeItem : MonoBehaviour
{
    [SerializeField] private Image imageStatIcon;
    [SerializeField] private TextMeshProUGUI textStatLevel;
    [SerializeField] private TextMeshProUGUI textStatName;
    [SerializeField] private TextMeshProUGUI textCurrentStat;
    [SerializeField] private Button buttonStatUpgrade;
    [SerializeField] private TextMeshProUGUI textConsumeCurrency;

    public Image StatIconImage => imageStatIcon;
    public TextMeshProUGUI StatLevelText => textStatLevel;
    public TextMeshProUGUI StatNameText => textStatName;
    public TextMeshProUGUI CurrentStatText => textCurrentStat;
    public Button UpgradeButton => buttonStatUpgrade;
    public TextMeshProUGUI ConsumeCurrencyText => textConsumeCurrency;
}
