using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Stage HUD 렌더링과 버튼 바인딩만 담당한다.
/// </summary>
public sealed class StageUIView
{
    private readonly TextMeshProUGUI popupStageLevelText;
    private readonly TextMeshProUGUI mapInfoStageNameText;
    private readonly TextMeshProUGUI mapInfoStageLevelText;
    private readonly TextMeshProUGUI progressText;
    private readonly Slider progressSlider;
    private readonly GameObject bossPanel;
    private readonly TextMeshProUGUI bossTimeText;
    private readonly TextMeshProUGUI bossHpText;
    private readonly Slider bossHpSlider;
    private readonly Button summonBossButton;
    private readonly Image summonBossButtonImage;
    private readonly Image summonBossIconImage;
    private readonly Color summonBossEnabledColor;
    private readonly Color summonBossDisabledColor;

    public StageUIView(
        TextMeshProUGUI popupStageLevelText,
        TextMeshProUGUI mapInfoStageNameText,
        TextMeshProUGUI mapInfoStageLevelText,
        TextMeshProUGUI progressText,
        Slider progressSlider,
        GameObject bossPanel,
        TextMeshProUGUI bossTimeText,
        TextMeshProUGUI bossHpText,
        Slider bossHpSlider,
        Button summonBossButton,
        Image summonBossButtonImage,
        Image summonBossIconImage,
        Color summonBossEnabledColor,
        Color summonBossDisabledColor)
    {
        this.popupStageLevelText = popupStageLevelText;
        this.mapInfoStageNameText = mapInfoStageNameText;
        this.mapInfoStageLevelText = mapInfoStageLevelText;
        this.progressText = progressText;
        this.progressSlider = progressSlider;
        this.bossPanel = bossPanel;
        this.bossTimeText = bossTimeText;
        this.bossHpText = bossHpText;
        this.bossHpSlider = bossHpSlider;
        this.summonBossButton = summonBossButton;
        this.summonBossButtonImage = summonBossButtonImage;
        this.summonBossIconImage = summonBossIconImage;
        this.summonBossEnabledColor = summonBossEnabledColor;
        this.summonBossDisabledColor = summonBossDisabledColor;
    }

    public void Render(int chapter, int stageNumber, string stageName, int currentKill, int maxKill)
    {
        SetStageLevel(chapter, stageNumber);
        SetStageName(stageName);
        SetStageProgress(currentKill, maxKill);
    }

    public void SetStageName(string stageName)
    {
        mapInfoStageNameText.text = string.IsNullOrEmpty(stageName) ? "-" : stageName;
    }

    public void SetStageLevel(int chapter, int stage)
    {
        string levelText = chapter > 0 && stage > 0 ? $"{chapter}-{stage}" : "-";
        popupStageLevelText.text = levelText;
        mapInfoStageLevelText.text = levelText;
    }

    public void SetStageProgress(int currentKill, int maxKill)
    {
        int clampedMax = Mathf.Max(0, maxKill);
        int clampedCurrent = Mathf.Clamp(currentKill, 0, clampedMax);
        int progressPercent = clampedMax > 0
            ? Mathf.RoundToInt((clampedCurrent / (float)clampedMax) * 100f)
            : 0;

        progressText.text = $"{progressPercent}%";
        progressSlider.minValue = 0f;
        progressSlider.maxValue = clampedMax > 0 ? clampedMax : 1f;
        progressSlider.SetValueWithoutNotify(clampedCurrent);
    }

    public void SetBossMode(bool active)
    {
        bool showBoss = active && bossPanel != null;
        ShowProgress(!showBoss);
        ShowBoss(showBoss);
    }

    public void SetBoss(float currentHp, float maxHp, float time)
    {
        if (bossTimeText != null)
            bossTimeText.text = FormatTimer(time);

        if (bossHpText != null)
            bossHpText.text = FormatPercent(currentHp, maxHp);

        if (bossHpSlider != null)
        {
            float clampedMax = Mathf.Max(1f, maxHp);
            bossHpSlider.minValue = 0f;
            bossHpSlider.maxValue = clampedMax;
            bossHpSlider.SetValueWithoutNotify(Mathf.Clamp(currentHp, 0f, clampedMax));
        }
    }

    public void ShowProgress(bool visible)
    {
        if (progressText != null)
            progressText.gameObject.SetActive(visible);
        if (progressSlider != null)
            progressSlider.gameObject.SetActive(visible);
    }

    public void ShowBoss(bool visible)
    {
        if (bossPanel != null)
            bossPanel.SetActive(visible);
    }

    public void BindSummonButton(UnityAction onClick)
    {
        summonBossButton.onClick.RemoveListener(onClick);
        summonBossButton.onClick.AddListener(onClick);
    }

    public void UnbindSummonButton(UnityAction onClick)
    {
        summonBossButton.onClick.RemoveListener(onClick);
    }

    public void SetSummonInteractable(bool interactable)
    {
        summonBossButton.interactable = interactable;
        summonBossButtonImage.color = interactable ? summonBossEnabledColor : summonBossDisabledColor;
        summonBossIconImage.color = interactable ? summonBossEnabledColor : summonBossDisabledColor;
    }

    private static string FormatTimer(float seconds)
    {
        return Mathf.Max(0f, seconds).ToString("0.000", CultureInfo.InvariantCulture);
    }

    private static string FormatPercent(float currentHp, float maxHp)
    {
        if (maxHp <= 0f)
            return "0%";

        float percent = Mathf.Clamp01(currentHp / maxHp) * 100f;
        return $"{Mathf.RoundToInt(percent)}%";
    }
}
