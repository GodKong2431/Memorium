using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum ActiveSkillItemVisualState
{
    NotEnough,
    Enough,
    Upgrade
}

public readonly struct ActiveSkillItemRenderData
{
    public readonly string SkillName;
    public readonly Sprite SkillIcon;
    public readonly int CurrentCount;
    public readonly int RequiredCount;
    public readonly int Level;
    public readonly bool CanClickAction;
    public readonly ActiveSkillItemVisualState State;

    public ActiveSkillItemRenderData(
        string skillName,
        Sprite skillIcon,
        int currentCount,
        int requiredCount,
        int level,
        bool canClickAction,
        ActiveSkillItemVisualState state)
    {
        SkillName = skillName;
        SkillIcon = skillIcon;
        CurrentCount = currentCount;
        RequiredCount = requiredCount;
        Level = level;
        CanClickAction = canClickAction;
        State = state;
    }
}

/// <summary>
/// ActiveSkill 아이템 1개의 렌더링과 상태 전환을 담당한다.
/// </summary>
public sealed class ActiveSkillItemView
{
    private readonly RectTransform root;
    private readonly LayoutElement layout;
    private readonly Image iconImage;
    private readonly TMP_Text nameLabel;
    private readonly GameObject lockedSharedRoot;
    private readonly GameObject notEnoughRoot;
    private readonly GameObject enoughRoot;
    private readonly GameObject upgradeRoot;
    private readonly TMP_Text lockedCountLabel;
    private readonly TMP_Text upgradeCountLabel;
    private readonly Button unlockButton;
    private readonly Button upgradeButton;
    private readonly TMP_Text levelLabel;

    private readonly float lockedHeight;
    private readonly float upgradeHeight;

    public ActiveSkillItemView(ActiveSkillItemBinding binding, float lockedHeight, float upgradeHeight)
    {
        if (binding == null)
            throw new System.ArgumentNullException(nameof(binding));

        root = binding.Root;
        layout = binding.Layout;
        iconImage = binding.IconImage;
        nameLabel = binding.NameLabel;
        lockedSharedRoot = binding.LockedSharedRoot;
        notEnoughRoot = binding.NotEnoughRoot;
        enoughRoot = binding.EnoughRoot;
        upgradeRoot = binding.UpgradeRoot;
        lockedCountLabel = binding.LockedCountLabel;
        upgradeCountLabel = binding.UpgradeCountLabel;
        unlockButton = binding.UnlockButton;
        upgradeButton = binding.UpgradeButton;
        levelLabel = binding.LevelLabel;
        this.lockedHeight = lockedHeight;
        this.upgradeHeight = upgradeHeight;
    }

    public void SetMergeClickHandler(UnityAction onClick)
    {
        SetClickHandler(unlockButton, onClick);
        SetClickHandler(upgradeButton, onClick);
    }

    public void Render(ActiveSkillItemRenderData data)
    {
        if (nameLabel != null)
            nameLabel.text = string.IsNullOrEmpty(data.SkillName) ? "-" : data.SkillName;

        if (iconImage != null && data.SkillIcon != null)
            iconImage.sprite = data.SkillIcon;

        int clampedCurrent = Mathf.Max(0, data.CurrentCount);
        int clampedRequired = Mathf.Max(1, data.RequiredCount);
        string countText = $"{clampedCurrent} / {clampedRequired}";

        if (lockedCountLabel != null)
            lockedCountLabel.text = countText;

        if (upgradeCountLabel != null)
            upgradeCountLabel.text = countText;

        if (levelLabel != null)
            levelLabel.text = $"LV. {Mathf.Max(0, data.Level)}";

        if (unlockButton != null)
            unlockButton.interactable = data.State == ActiveSkillItemVisualState.Enough && data.CanClickAction;

        if (upgradeButton != null)
            upgradeButton.interactable = data.State == ActiveSkillItemVisualState.Upgrade && data.CanClickAction;

        SetVisualState(data.State);
    }

    private void SetVisualState(ActiveSkillItemVisualState state)
    {
        bool isUpgrade = state == ActiveSkillItemVisualState.Upgrade;

        SetActive(lockedSharedRoot, !isUpgrade);
        SetActive(notEnoughRoot, state == ActiveSkillItemVisualState.NotEnough);
        SetActive(enoughRoot, state == ActiveSkillItemVisualState.Enough);
        SetActive(upgradeRoot, isUpgrade);

        SetHeight(isUpgrade ? upgradeHeight : lockedHeight);
    }

    private void SetHeight(float height)
    {
        if (layout != null)
        {
            layout.minHeight = height;
            layout.preferredHeight = height;
            layout.flexibleHeight = 0f;
        }

        if (root == null)
            return;

        Vector2 size = root.sizeDelta;
        if (!Mathf.Approximately(size.y, height))
        {
            size.y = height;
            root.sizeDelta = size;
        }
    }

    private static void SetActive(GameObject target, bool value)
    {
        if (target != null && target.activeSelf != value)
            target.SetActive(value);
    }

    private static void SetClickHandler(Button button, UnityAction onClick)
    {
        if (button == null || onClick == null)
            return;

        button.onClick.RemoveListener(onClick);
        button.onClick.AddListener(onClick);
    }
}
