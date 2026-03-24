using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 스탯 업그레이드 한 행의 렌더링과 버튼 이벤트 바인딩을 담당한다.
/// </summary>
public sealed class PlayerStatUpgradeItemView
{
    private readonly PlayerStatUpgradeItem item;

    public PlayerStatUpgradeItemView(PlayerStatUpgradeItem item)
    {
        this.item = item;
    }

    public void BindUpgradeButton(UnityAction onClick)
    {
        HoldAcceleratorAddon.Ensure(item.UpgradeButton);

        // 재바인딩 시 중복 리스너가 쌓이지 않도록 먼저 제거한다.
        item.UpgradeButton.onClick.RemoveListener(onClick);
        item.UpgradeButton.onClick.AddListener(onClick);
    }

    public void Render(
        Sprite icon,
        string statLevelText,
        string statNameText,
        string currentStatText,
        string consumeCurrencyText,
        bool upgradeInteractable,
        Color consumeCurrencyColor)
    {
        item.StatIconImage.sprite = icon;
        item.StatLevelText.text = statLevelText;
        item.StatNameText.text = statNameText;
        item.CurrentStatText.text = currentStatText;
        item.ConsumeCurrencyText.text = consumeCurrencyText;
        item.ConsumeCurrencyText.color = consumeCurrencyColor;
        item.UpgradeButton.interactable = upgradeInteractable;
    }
}
