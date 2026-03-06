using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class EquipItemView
{
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
    private const float DimFactor = 0.35f;

    private readonly EquipItemUI ui;
    private readonly List<Image> stars = new List<Image>();

    private readonly Color iconBaseColor;
    private readonly Color levelBaseColor;
    private readonly Color countBaseColor;
    private readonly Color needBaseColor;

    private Color tierColor = Color.white;
    private Color frameColor = Color.white;
    private bool isDimmed;

    public EquipItemView(EquipItemUI ui)
    {
        this.ui = ui;
        stars.Add(ui.TierStar);

        iconBaseColor = ui.Icon.color;
        levelBaseColor = ui.LevelText.color;
        countBaseColor = ui.CurrentCountText.color;
        needBaseColor = ui.NeedCountText.color;
    }

    public void Bind(UnityAction onClick)
    {
        // 버튼 동작은 현재 콜백 하나만 유지한다.
        ui.Button.onClick.RemoveAllListeners();
        ui.Button.onClick.AddListener(onClick);
    }

    public void Render(Sprite icon, string levelText, int starCount, Color tierColor)
    {
        ui.Icon.sprite = icon;
        ui.LevelText.text = levelText;
        ui.TierPanel.color = Transparent;

        int safeStarCount = Mathf.Max(1, starCount);
        this.tierColor = tierColor;

        SyncStars(safeStarCount);
        ApplyDimState();
    }

    public void RenderCount(int count, int mergeCount)
    {
        int required = Mathf.Max(1, mergeCount);
        int owned = Mathf.Max(0, count);
        int value = Mathf.Clamp(owned, 0, required);

        ui.MergeSlider.minValue = 0f;
        ui.MergeSlider.maxValue = required;
        ui.MergeSlider.SetValueWithoutNotify(value);
        ui.CurrentCountText.text = FormatCount(owned);
        ui.NeedCountText.text = required.ToString();
    }

    public void SetFrameColor(Color color)
    {
        frameColor = color;
        ApplyDimState();
    }

    public void SetDimmed(bool dimmed)
    {
        // 잠금 상태는 클릭 가능 여부와 시각 톤을 함께 제어한다.
        isDimmed = dimmed;
        ui.Button.interactable = !dimmed;
        ApplyDimState();
    }

    private void SyncStars(int required)
    {
        while (stars.Count < required)
        {
            Image clone = Object.Instantiate(stars[0], ui.TierRoot);
            clone.name = $"(Img)TierStar_{stars.Count + 1}";
            stars.Add(clone);
        }

        for (int i = 0; i < stars.Count; i++)
            stars[i].gameObject.SetActive(i < required);
    }

    private void ApplyDimState()
    {
        float factor = isDimmed ? DimFactor : 1f;
        Color appliedTierColor = Multiply(tierColor, factor);
        Color appliedFrameColor = Multiply(frameColor, factor);

        ui.Icon.color = Multiply(iconBaseColor, factor);
        ui.LevelText.color = Multiply(levelBaseColor, factor);
        ui.CurrentCountText.color = Multiply(countBaseColor, factor);
        ui.NeedCountText.color = Multiply(needBaseColor, factor);

        Image[] frames = ui.Frames;
        for (int i = 0; i < frames.Length; i++)
            frames[i].color = appliedFrameColor;

        for (int i = 0; i < stars.Count; i++)
        {
            if (!stars[i].gameObject.activeSelf)
                continue;

            stars[i].color = appliedTierColor;
        }
    }

    private static string FormatCount(int count)
    {
        return count > 999 ? "999+" : count.ToString();
    }

    private static Color Multiply(Color color, float factor)
    {
        return new Color(color.r * factor, color.g * factor, color.b * factor, color.a);
    }
}

