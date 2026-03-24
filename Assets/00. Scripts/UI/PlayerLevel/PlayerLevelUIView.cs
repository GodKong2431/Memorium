using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plain view for player level progress UI.
/// </summary>
public sealed class PlayerLevelUIView
{
    private readonly TextMeshProUGUI progressText;
    private readonly Slider expBar;
    private readonly TextMeshProUGUI playerLevelText;

    public PlayerLevelUIView(
        TextMeshProUGUI progressText,
        Slider expBar,
        TextMeshProUGUI playerLevelText)
    {
        this.progressText = progressText;
        this.expBar = expBar;
        this.playerLevelText = playerLevelText;
    }

    public void Render(int level, float progress01)
    {
        float clamped = Mathf.Clamp01(progress01);
        SetProgress(clamped);
        SetProgressText(clamped);
        SetLevel(level);
    }

    private void SetProgress(float progress01)
    {
        expBar.minValue = 0f;
        expBar.maxValue = 1f;
        expBar.SetValueWithoutNotify(progress01);
    }

    private void SetProgressText(float progress01)
    {
        int percent = Mathf.RoundToInt(progress01 * 100f);
        progressText.text = $"{percent}%";
    }

    private void SetLevel(int level)
    {
        playerLevelText.text = level > 0
            ? $"{level}"
            : "-";
    }
}
