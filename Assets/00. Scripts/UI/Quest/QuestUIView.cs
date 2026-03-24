using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Quest HUD 렌더링과 버튼 바인딩만 담당한다.
/// </summary>
public sealed class QuestUIView
{
    private readonly TextMeshProUGUI textQuestName;
    private readonly TextMeshProUGUI textQuestNumber;
    private readonly GameObject questProgressSliderRoot;
    private readonly Slider questProgressSlider;
    private readonly Image questRewardImage;
    private readonly TextMeshProUGUI rewardCountText;
    private readonly Button rewardTouchButton;
    private readonly TextMeshProUGUI rewardTouchText;

    public QuestUIView(
        TextMeshProUGUI textQuestName,
        TextMeshProUGUI textQuestNumber,
        GameObject questProgressSliderRoot,
        Slider questProgressSlider,
        Image questRewardImage,
        TextMeshProUGUI rewardCountText,
        Button rewardTouchButton,
        TextMeshProUGUI rewardTouchText)
    {
        this.textQuestName = textQuestName;
        this.textQuestNumber = textQuestNumber;
        this.questProgressSliderRoot = questProgressSliderRoot;
        this.questProgressSlider = questProgressSlider;
        this.questRewardImage = questRewardImage;
        this.rewardCountText = rewardCountText;
        this.rewardTouchButton = rewardTouchButton;
        this.rewardTouchText = rewardTouchText;
    }

    public void SetQuestInfo(string numberText, string titleText)
    {
        textQuestNumber.text = numberText;
        textQuestName.text = titleText;
    }

    public void SetProgress(float progress01)
    {
        questProgressSlider.SetValueWithoutNotify(progress01);
    }

    public void SetRewardSprite(Sprite sprite)
    {
        questRewardImage.sprite = sprite;
    }

    public void SetRewardCountText(string text)
    {
        if (rewardCountText != null)
            rewardCountText.text = text ?? string.Empty;
    }

    public void SetRewardCountVisible(bool visible)
    {
        if (rewardCountText != null)
            rewardCountText.gameObject.SetActive(visible);
    }

    public void SetProgressVisible(bool visible)
    {
        questProgressSliderRoot.SetActive(visible);
    }

    public void SetRewardButtonVisible(bool visible)
    {
        rewardTouchButton.gameObject.SetActive(visible);
    }

    public void SetRewardButtonInteractable(bool interactable)
    {
        rewardTouchButton.interactable = interactable;
    }

    public void SetRewardTouchText(string text)
    {
        rewardTouchText.text = text;
    }

    public void BindRewardButton(UnityAction onClick)
    {
        rewardTouchButton.onClick.RemoveListener(onClick);
        rewardTouchButton.onClick.AddListener(onClick);
    }

    public void UnbindRewardButton(UnityAction onClick)
    {
        rewardTouchButton.onClick.RemoveListener(onClick);
    }
}
