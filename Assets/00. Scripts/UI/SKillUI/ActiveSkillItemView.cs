using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ActiveSkillItemVisualState
{
    NotEnough,
    Enough,
    Upgrade
}

public readonly struct ActiveSkillItemGemSlotDisplayData
{
    public readonly bool IsUnlocked;
    public readonly bool HasEquippedGem;
    public readonly Sprite EquippedGemIcon;

    public ActiveSkillItemGemSlotDisplayData(bool isUnlocked, bool hasEquippedGem, Sprite equippedGemIcon)
    {
        IsUnlocked = isUnlocked;
        HasEquippedGem = hasEquippedGem;
        EquippedGemIcon = equippedGemIcon;
    }
}

public readonly struct ActiveSkillItemDisplayData
{
    public readonly int SkillId;
    public readonly string SkillName;
    public readonly Sprite Icon;
    public readonly int Level;
    public readonly int OpenGemCount;
    public readonly bool CanEquip;
    public readonly ActiveSkillItemVisualState VisualState;
    public readonly int CurrentCount;
    public readonly int RequiredCount;
    public readonly bool CanTriggerStateAction;
    public readonly ActiveSkillItemGemSlotDisplayData[] UpgradeGemSlots;
    public readonly bool CanLevelUp;
    public readonly string LevelUpCostString;
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
        bool canTriggerStateAction,
        ActiveSkillItemGemSlotDisplayData[] upgradeGemSlots,
        bool canLevelUp,
        string levelUpCostString
       )
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
        UpgradeGemSlots = upgradeGemSlots;
        CanLevelUp = canLevelUp;
        LevelUpCostString = levelUpCostString;
    }
}

public sealed class ActiveSkillItemView
{
    private readonly ActiveSkillItemBinding binding;

    private int currentSkillId = -1;
    private Action<int> onSkillClick;
    private Action<int> onEquipClick;
    private Action<int> onStateActionClick;
    private Action<int> onLevelUpClick;

    public ActiveSkillItemView(ActiveSkillItemBinding binding)
    {
        this.binding = binding;
        this.binding.EnsureReferences();
        BindButtonsOnce();
    }

    public void Bind(
        ActiveSkillItemDisplayData data,
        Action<int> skillClickHandler,
        Action<int> equipClickHandler,
        Action<int> stateActionClickHandler,
        Action<int> levelUpClickHandler)
    {
        binding.EnsureReferences();
        currentSkillId = data.SkillId;
        onSkillClick = skillClickHandler;
        onEquipClick = equipClickHandler;
        onStateActionClick = stateActionClickHandler;
        onLevelUpClick = levelUpClickHandler;

        if (binding.SkillIconDisplay != null)
            binding.SkillIconDisplay.sprite = data.Icon;

        if (binding.IconImage != null)
            binding.IconImage.sprite = data.Icon;

        if (binding.NameLabel != null)
            binding.NameLabel.text = data.SkillName;

        if (binding.LevelUpButton != null)
        {
            bool canLevelUp = data.VisualState == ActiveSkillItemVisualState.Upgrade
                && data.CanLevelUp;
            binding.LevelUpButton.interactable = canLevelUp;
        }

        SetLevelText(binding.IconLevelLabel, data.Level);
        SetLevelText(binding.LevelLabel, data.Level);
        SetGemIcons(data.OpenGemCount);
        SetUpgradeGemSlots(data.UpgradeGemSlots);
        SetVisualState(data.VisualState);
        SetCountLabels(data);
        SetStateButtonInteractable(data);

        if (binding.EquipButton != null)
            binding.EquipButton.interactable = data.CanEquip;

        if (binding.LevelUpCostText != null)
            binding.LevelUpCostText.SetText(data.LevelUpCostString ?? "");
    }

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

        if (binding.LevelUpButton != null)
        {
            binding.LevelUpButton.onClick.RemoveListener(HandleLevelUpClick);
            binding.LevelUpButton.onClick.AddListener(HandleLevelUpClick);
        }
    }

    private void HandleLevelUpClick()
    {
        if (currentSkillId >= 0)
            onLevelUpClick?.Invoke(currentSkillId);
    }
    private void HandleSkillClick()
    {
        if (currentSkillId >= 0)
            onSkillClick?.Invoke(currentSkillId);
    }

    private void HandleEquipClick()
    {
        if (currentSkillId >= 0)
            onEquipClick?.Invoke(currentSkillId);
    }

    private void HandleStateActionClick()
    {
        if (currentSkillId >= 0)
            onStateActionClick?.Invoke(currentSkillId);
    }

    private void SetGemIcons(int openGemCount)
    {
        if (binding.GemPanelRoot != null)
            binding.GemPanelRoot.gameObject.SetActive(openGemCount > 0);

        if (binding.GemPanelRoot == null)
            return;

        for (int i = 0; i < binding.GemPanelRoot.childCount; i++)
            binding.GemPanelRoot.GetChild(i).gameObject.SetActive(i < openGemCount);
    }

    private void SetUpgradeGemSlots(ActiveSkillItemGemSlotDisplayData[] slots)
    {
        RectTransform[] slotRoots = binding.UpgradeGemSlotRoots;
        Image[] gemImages = binding.UpgradeGemImages;
        GameObject[] lockObjects = binding.UpgradeGemLockObjects;
        if (slotRoots == null || gemImages == null || lockObjects == null)
            return;

        int slotCount = Mathf.Min(slotRoots.Length, Mathf.Min(gemImages.Length, lockObjects.Length));
        for (int i = 0; i < slotCount; i++)
        {
            RectTransform slotRoot = slotRoots[i];
            if (slotRoot == null)
                continue;

            ActiveSkillItemGemSlotDisplayData slot = slots != null && i < slots.Length
                ? slots[i]
                : default;

            slotRoot.gameObject.SetActive(true);

            Image gemImage = gemImages[i];
            if (gemImage != null)
            {
                gemImage.sprite = slot.HasEquippedGem ? slot.EquippedGemIcon : null;

                gemImage.gameObject.SetActive(slot.HasEquippedGem);
            }

            GameObject lockObject = lockObjects[i];
            if (lockObject != null)
                lockObject.SetActive(!slot.IsUnlocked);
        }
    }

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

    private void SetCountLabels(ActiveSkillItemDisplayData data)
    {
        string countText = $"{data.CurrentCount}/{data.RequiredCount}";

        if (binding.LockedCountLabel != null)
            binding.LockedCountLabel.text = countText;

        if (binding.UpgradeCountLabel != null)
            binding.UpgradeCountLabel.text = countText;
    }

    private void SetStateButtonInteractable(ActiveSkillItemDisplayData data)
    {
        if (binding.UnlockButton != null)
        {
            binding.UnlockButton.interactable = data.VisualState == ActiveSkillItemVisualState.Enough
                && data.CanTriggerStateAction;
        }

        if (binding.UpgradeButton != null)
        {
            binding.UpgradeButton.interactable = data.VisualState == ActiveSkillItemVisualState.Upgrade
                && data.CanTriggerStateAction;
        }
    }

    private static void SetLevelText(TMP_Text label, int level)
    {
        if (label == null)
            return;

        label.text = level > 0 ? $"Lv.{level}" : string.Empty;
    }
}
