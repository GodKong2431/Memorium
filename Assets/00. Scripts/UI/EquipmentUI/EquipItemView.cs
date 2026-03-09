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
        stars.Add(ui.TierStar);
    }

    public void Bind(UnityAction onClick)
    {
        ui.Button.onClick.RemoveAllListeners();
        ui.Button.onClick.AddListener(onClick);
    }

    public void Render(Sprite icon, string levelText, int starCount, Color tierColor)
    {
        ui.Icon.sprite = icon;
        ui.LevelText.text = levelText;
        ui.TierPanel.color = Transparent;

        SyncStars(Mathf.Max(1, starCount), tierColor);
    }

    public void RenderCount(int count, int mergeCount)
    {
        int required = Mathf.Max(1, mergeCount);
        int owned = Mathf.Max(0, count);

        ui.MergeSlider.minValue = 0f;
        ui.MergeSlider.maxValue = required;
        ui.MergeSlider.SetValueWithoutNotify(Mathf.Clamp(owned, 0, required));
        ui.CurrentCountText.text = owned > 999 ? "999+" : owned.ToString();
        ui.NeedCountText.text = required.ToString();
    }

    public void SetFrameColor(Color color)
    {
        Image[] frames = ui.Frames;
        for (int i = 0; i < frames.Length; i++)
            frames[i].color = color;
    }

    public void SetDimmed(bool dimmed)
    {
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

    private void SyncStars(int required, Color color)
    {
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
    }
}
