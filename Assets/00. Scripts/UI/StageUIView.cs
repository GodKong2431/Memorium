using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 스테이지 HUD(라벨, 진행도, 보스 소환 버튼) 표시를 담당하는 UI 뷰 래퍼.
/// </summary>
public sealed class StageUIView
{
    private readonly TextMeshProUGUI popupStageLevelText;
    private readonly TextMeshProUGUI mapInfoStageNameText;
    private readonly TextMeshProUGUI mapInfoStageLevelText;
    private readonly TextMeshProUGUI progressText;
    private readonly Slider progressSlider;
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
        this.summonBossButton = summonBossButton;
        this.summonBossButtonImage = summonBossButtonImage;
        this.summonBossIconImage = summonBossIconImage;
        this.summonBossEnabledColor = summonBossEnabledColor;
        this.summonBossDisabledColor = summonBossDisabledColor;
    }

    

    // 스테이지 이름(지역명)을 MapInfo에 갱신한다.
    public void SetStageName(string stageName)
    {
        mapInfoStageNameText.text = stageName;
    }

    // 스테이지 레벨(층-스테이지)을 팝업/MapInfo 양쪽에 동일하게 반영한다.
    public void SetStageLevel(int chapter, int stage)
    {
        string levelText = $"{chapter}-{stage}";
        popupStageLevelText.text = levelText;
        mapInfoStageLevelText.text = levelText;
    }

    // 처치 진행도를 텍스트/슬라이더에 반영하고 보스 버튼 상태를 갱신한다.
    public void SetStageProgress(int currentKill, int maxKill)
    {
        // int clampedCurrent = Mathf.Max(0, currentKill);
        // int clampedMax = Mathf.Max(0, maxKill);

        if (maxKill > 0 && currentKill > maxKill)
            currentKill = maxKill;

        int progressPercent = maxKill > 0
            ? Mathf.RoundToInt((currentKill / (float)maxKill) * 100f)
            : 0;
        progressText.text = $"{progressPercent}%";
        progressSlider.minValue = 0f;
        progressSlider.maxValue = maxKill > 0 ? maxKill : 1f;
        progressSlider.value = currentKill;

        SetSummonInteractable(maxKill > 0 && currentKill >= maxKill);
    }

    // 버튼 클릭 액션을 연결한다.
    public void BindSummonButton(UnityAction onClick)
    {
        summonBossButton.onClick.RemoveListener(onClick);
        summonBossButton.onClick.AddListener(onClick);
    }

    // 버튼 클릭 액션을 해제한다.
    public void UnbindSummonButton(UnityAction onClick)
    {
        summonBossButton.onClick.RemoveListener(onClick);
    }

    // 보스 소환 버튼의 활성/비활성 상태를 단일 버튼 색상으로 전환한다.
    public void SetSummonInteractable(bool interactable)
    {
        summonBossButton.interactable = interactable;
        summonBossButtonImage.color = interactable ? summonBossEnabledColor : summonBossDisabledColor;
        summonBossIconImage.color = interactable ? summonBossEnabledColor : summonBossDisabledColor;
    }
}
