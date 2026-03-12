using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스킬 아이템 카드가 가질 수 있는 표시 상태입니다.
/// </summary>
public enum ActiveSkillItemVisualState
{
    // 조각이 부족한 잠금 상태입니다.
    NotEnough,
    // 잠금 해제가 가능한 상태입니다.
    Enough,
    // 이미 소유 중이며 승급 가능한 상태입니다.
    Upgrade
}

/// <summary>
/// 스킬 아이템 카드에 그릴 표시 데이터를 묶은 값 타입입니다.
/// </summary>
public readonly struct ActiveSkillItemDisplayData
{
    // 표시 대상 스킬 ID입니다.
    public readonly int SkillId;
    // 표시 대상 스킬 이름입니다.
    public readonly string SkillName;
    // 표시할 스킬 아이콘입니다.
    public readonly Sprite Icon;
    // 표시할 스킬 레벨입니다.
    public readonly int Level;
    // 열린 젬 슬롯 개수입니다.
    public readonly int OpenGemCount;
    // 장착 버튼을 활성화할지 여부입니다.
    public readonly bool CanEquip;
    // 현재 카드의 시각 상태입니다.
    public readonly ActiveSkillItemVisualState VisualState;
    // 현재 조각 수입니다.
    public readonly int CurrentCount;
    // 필요한 조각 수입니다.
    public readonly int RequiredCount;
    // 잠금 해제/승급 버튼을 눌러도 되는지 여부입니다.
    public readonly bool CanTriggerStateAction;

    // 카드 표시 데이터 전체를 초기화합니다.
    public ActiveSkillItemDisplayData(
        int skillId,
        string skillName,
        Sprite icon,
        int level,
        int openGemCount,
        bool canEquip,
        ActiveSkillItemVisualState visualState,
        int currentCount,
        int requiredCount,
        bool canTriggerStateAction)
    {
        SkillId = skillId;
        SkillName = skillName;
        Icon = icon;
        Level = level;
        OpenGemCount = openGemCount;
        CanEquip = canEquip;
        VisualState = visualState;
        CurrentCount = currentCount;
        RequiredCount = requiredCount;
        CanTriggerStateAction = canTriggerStateAction;
    }
}

/// <summary>
/// 액티브 스킬 아이템 한 개의 텍스트, 아이콘, 버튼 상태를 갱신합니다.
/// </summary>
public sealed class ActiveSkillItemView
{
    // 프리팹에서 받아온 UI 참조 묶음입니다.
    private readonly ActiveSkillItemBinding binding;

    // 현재 카드가 가리키는 스킬 ID입니다.
    private int currentSkillId = -1;
    // 아이콘 클릭 시 호출할 콜백입니다.
    private Action<int> onSkillClick;
    // 장착 버튼 클릭 시 호출할 콜백입니다.
    private Action<int> onEquipClick;
    // 잠금 해제/승급 버튼 클릭 시 호출할 콜백입니다.
    private Action<int> onStateActionClick;

    // 카드 뷰를 바인딩 참조와 함께 생성합니다.
    public ActiveSkillItemView(ActiveSkillItemBinding binding)
    {
        this.binding = binding;
        BindButtonsOnce();
    }

    // 전달받은 데이터로 카드 전체 표시를 갱신합니다.
    public void Bind(
        ActiveSkillItemDisplayData data,
        Action<int> skillClickHandler,
        Action<int> equipClickHandler,
        Action<int> stateActionClickHandler)
    {
        currentSkillId = data.SkillId;
        onSkillClick = skillClickHandler;
        onEquipClick = equipClickHandler;
        onStateActionClick = stateActionClickHandler;

        if (binding.SkillIconDisplay != null)
            binding.SkillIconDisplay.sprite = data.Icon;

        if (binding.IconImage != null)
            binding.IconImage.sprite = data.Icon;

        if (binding.NameLabel != null)
            binding.NameLabel.text = data.SkillName;

        SetLevelText(binding.IconLevelLabel, data.Level);
        SetLevelText(binding.LevelLabel, data.Level);
        SetGemIcons(data.OpenGemCount);
        SetVisualState(data.VisualState);
        SetCountLabels(data);
        SetStateButtonInteractable(data);

        if (binding.EquipButton != null)
            binding.EquipButton.interactable = data.CanEquip;
    }

    // 버튼 리스너를 한 번만 연결합니다.
    private void BindButtonsOnce()
    {
        if (binding.SkillButton != null)
        {
            binding.SkillButton.onClick.RemoveListener(HandleSkillClick);
            binding.SkillButton.onClick.AddListener(HandleSkillClick);
        }

        if (binding.EquipButton != null)
        {
            binding.EquipButton.onClick.RemoveListener(HandleEquipClick);
            binding.EquipButton.onClick.AddListener(HandleEquipClick);
        }

        if (binding.UnlockButton != null)
        {
            binding.UnlockButton.onClick.RemoveListener(HandleStateActionClick);
            binding.UnlockButton.onClick.AddListener(HandleStateActionClick);
        }

        if (binding.UpgradeButton != null)
        {
            binding.UpgradeButton.onClick.RemoveListener(HandleStateActionClick);
            binding.UpgradeButton.onClick.AddListener(HandleStateActionClick);
        }
    }

    // 아이콘 버튼 클릭을 외부로 전달합니다.
    private void HandleSkillClick()
    {
        if (currentSkillId >= 0)
            onSkillClick?.Invoke(currentSkillId);
    }

    // 장착 버튼 클릭을 외부로 전달합니다.
    private void HandleEquipClick()
    {
        if (currentSkillId >= 0)
            onEquipClick?.Invoke(currentSkillId);
    }

    // 잠금 해제/승급 버튼 클릭을 외부로 전달합니다.
    private void HandleStateActionClick()
    {
        if (currentSkillId >= 0)
            onStateActionClick?.Invoke(currentSkillId);
    }

    // 열린 젬 슬롯 개수만큼 아이콘을 표시합니다.
    private void SetGemIcons(int openGemCount)
    {
        if (binding.GemPanelRoot != null)
            binding.GemPanelRoot.gameObject.SetActive(openGemCount > 0);

        if (binding.GemPanelRoot == null)
            return;

        for (int i = 0; i < binding.GemPanelRoot.childCount; i++)
            binding.GemPanelRoot.GetChild(i).gameObject.SetActive(i < openGemCount);
    }

    // 현재 카드 상태에 맞는 루트 오브젝트만 표시합니다.
    private void SetVisualState(ActiveSkillItemVisualState state)
    {
        if (binding.LockedSharedRoot != null)
            binding.LockedSharedRoot.SetActive(state != ActiveSkillItemVisualState.Upgrade);

        if (binding.NotEnoughRoot != null)
            binding.NotEnoughRoot.SetActive(state == ActiveSkillItemVisualState.NotEnough);

        if (binding.EnoughRoot != null)
            binding.EnoughRoot.SetActive(state == ActiveSkillItemVisualState.Enough);

        if (binding.UpgradeRoot != null)
            binding.UpgradeRoot.SetActive(state == ActiveSkillItemVisualState.Upgrade);
    }

    // 조각 수 텍스트를 상태별 라벨에 반영합니다.
    private void SetCountLabels(ActiveSkillItemDisplayData data)
    {
        string countText = $"{data.CurrentCount}/{data.RequiredCount}";

        if (binding.LockedCountLabel != null)
            binding.LockedCountLabel.text = countText;

        if (binding.UpgradeCountLabel != null)
            binding.UpgradeCountLabel.text = countText;
    }

    // 잠금 해제/승급 버튼의 interactable 상태를 갱신합니다.
    private void SetStateButtonInteractable(ActiveSkillItemDisplayData data)
    {
        if (binding.UnlockButton != null)
            binding.UnlockButton.interactable = data.VisualState == ActiveSkillItemVisualState.Enough
                && data.CanTriggerStateAction;

        if (binding.UpgradeButton != null)
            binding.UpgradeButton.interactable = data.VisualState == ActiveSkillItemVisualState.Upgrade
                && data.CanTriggerStateAction;
    }

    // 레벨 텍스트를 공통 포맷으로 설정합니다.
    private static void SetLevelText(TMP_Text label, int level)
    {
        if (label == null)
            return;

        label.text = level > 0 ? $"Lv.{level}" : string.Empty;
    }
}
