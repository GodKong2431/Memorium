using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public sealed class StoneInfoPanelUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI summaryText;
    [SerializeField] private TextMeshProUGUI currentUpgradeValueText;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private RectTransform panelRoot;

    public event Action OutsideClicked;

    public TextMeshProUGUI TitleText => titleText;
    public TextMeshProUGUI SummaryText => summaryText;
    public TextMeshProUGUI CurrentUpgradeValueText => currentUpgradeValueText;
    public RectTransform ContentRoot => contentRoot;
    public RectTransform PanelRoot => panelRoot;

    public void OnPointerClick(PointerEventData eventData)
    {
        // 본문 박스 바깥을 누르면 패널을 닫을 수 있게 이벤트를 보낸다.
        if (eventData == null || panelRoot == null)
        {
            return;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(panelRoot, eventData.position, eventData.pressEventCamera))
        {
            OutsideClicked?.Invoke();
        }
    }
}
