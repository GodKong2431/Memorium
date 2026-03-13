using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class EquipItemView
{
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
    private const float DimAlpha = 0.35f;

    private readonly EquipItemUI ui;
    private readonly List<Image> stars = new List<Image>();
    private CanvasGroup canvasGroup;

    public EquipItemView(EquipItemUI ui)
    {
        this.ui = ui;
        this.ui.EnsureBindings();

        if (this.ui.TierStar != null)
            stars.Add(this.ui.TierStar);
    }

    public void Bind(UnityAction onClick)
    {
        if (ui.Button == null)
            return;

        ui.Button.onClick.RemoveAllListeners();
        ui.Button.onClick.AddListener(onClick);
    }

    public void Render(Sprite icon, string levelText, int starCount, Color tierColor)
    {
        if (ui.Icon != null)
            ui.Icon.sprite = icon;

        SetLevelText(levelText);

        if (ui.TierPanel != null)
            ui.TierPanel.color = Transparent;

        SyncStars(Mathf.Max(1, starCount), tierColor);
    }

    public void RenderLevel(string levelText)
    {
        SetLevelText(levelText);
    }

    public void RenderCount(int count, int mergeCount)
    {
        int required = Mathf.Max(1, mergeCount);
        int owned = Mathf.Max(0, count);

        if (ui.MergeSlider != null)
        {
            ui.MergeSlider.gameObject.SetActive(true);
            ui.MergeSlider.minValue = 0f;
            ui.MergeSlider.maxValue = required;
            ui.MergeSlider.SetValueWithoutNotify(Mathf.Clamp(owned, 0, required));
        }

        if (ui.CurrentCountText != null)
            ui.CurrentCountText.text = owned >= 100 ? "99+" : owned.ToString();

        if (ui.NeedCountText != null)
            ui.NeedCountText.text = required.ToString();
    }

    public void SetFrameColor(Color color)
    {
        Image[] frameImages = ui.Frames;
        if (frameImages == null)
            return;

        for (int i = 0; i < frameImages.Length; i++)
        {
            if (frameImages[i] != null)
                frameImages[i].color = color;
        }
    }

    public void SetDimmed(bool dimmed)
    {
        if (ui.Button != null)
            ui.Button.interactable = !dimmed;

        if (canvasGroup == null)
        {
            canvasGroup = ui.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = ui.gameObject.AddComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = dimmed ? DimAlpha : 1f;
    }

    private void SetLevelText(string levelText)
    {
        bool hasLevelText = !string.IsNullOrWhiteSpace(levelText);

        if (ui.LevelDisplayRoot != null)
            ui.LevelDisplayRoot.gameObject.SetActive(hasLevelText);

        if (ui.LevelText != null)
            ui.LevelText.text = hasLevelText ? levelText : string.Empty;
    }

    private void SyncStars(int required, Color color)
    {
        if (stars.Count == 0 && ui.TierStar != null)
            stars.Add(ui.TierStar);

        if (stars.Count == 0 || ui.TierRoot == null)
            return;

        while (stars.Count < required)
        {
            Image clone = Object.Instantiate(stars[0], ui.TierRoot);
            clone.name = $"(Img)TierStar_{stars.Count + 1}";
            stars.Add(clone);
        }

        for (int i = 0; i < stars.Count; i++)
        {
            bool active = i < required;
            stars[i].gameObject.SetActive(active);
            if (active)
                stars[i].color = color;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(ui.TierRoot);
    }
}
