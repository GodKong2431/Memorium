using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class StoneInfoPanelUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private TextMeshProUGUI currentUpgradeValueText;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform panelRoot;

    public TextMeshProUGUI TitleText => titleText;
    public TextMeshProUGUI SummaryText => summaryText;
    public TextMeshProUGUI CurrentUpgradeValueText => currentUpgradeValueText;
    public RectTransform ContentRoot => contentRoot;
    public RectTransform PanelRoot => panelRoot;
}
