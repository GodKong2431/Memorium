using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 장착 슬롯 하나의 아이콘, 레벨, 젬, 쿨타임을 표시하는 뷰입니다.
/// </summary>
public class BattleSkillSlotView : MonoBehaviour
{
    [Header("References")]
    // 슬롯 클릭 입력을 받는 버튼입니다.
    [SerializeField] private Button rootButton;
    // 스킬 아이콘 이미지를 표시합니다.
    [SerializeField] private Image skillIcon;
    [SerializeField] private Sprite emptySprite;
    // 쿨타임 마스크 fill 이미지를 표시합니다.
    [SerializeField] private Image cooldownFill;
    // 남은 쿨타임 숫자 텍스트입니다.
    [SerializeField] private TMP_Text cooldownText;
    // 장착 스킬 레벨 텍스트입니다.
    [SerializeField] private TMP_Text levelLabel;
    // 젬 슬롯 아이콘 묶음의 루트입니다.
    [SerializeField] private RectTransform gemPanelRoot;

    [SerializeField] private Image[] gemSlotImages;

    // 외부에서 주입하는 슬롯 클릭 콜백입니다.
    private Action clickListener;
    // 빈 슬롯일 때 되돌릴 기본 아이콘입니다.
    // 빈 슬롯일 때 되돌릴 기본 아이콘 색상입니다.
    private Color defaultColor = Color.white;
    // 강조 상태 해제 시 복원할 버튼 색상입니다.
    private ColorBlock defaultButtonColors;
    // 현재 슬롯에 표시 중인 스킬 ID입니다.
    private int currentSkillId = -1;

    // 슬롯의 버튼 참조를 외부에 노출합니다.
    public Button RootButton => rootButton;
    // 현재 슬롯이 가리키는 스킬 ID를 외부에 노출합니다.
    public int CurrentSkillId => currentSkillId;

    // 기본 상태를 캐시하고 버튼/쿨타임 표시를 초기화합니다.
    private void Awake()
    {
        if (rootButton == null)
            rootButton = GetComponent<Button>();

        if (skillIcon != null)
        {
            defaultColor = skillIcon.color;
        }

        if (cooldownFill != null)
        {
            cooldownFill.raycastTarget = false;
            cooldownFill.fillAmount = 0f;
            cooldownFill.gameObject.SetActive(false);
        }

        if (cooldownText != null)
        {
            cooldownText.raycastTarget = false;
            cooldownText.SetText(string.Empty);
        }

        if (rootButton != null)
        {
            defaultButtonColors = rootButton.colors;
            rootButton.onClick.RemoveListener(HandleSlotClick);
            rootButton.onClick.AddListener(HandleSlotClick);
            UiButtonSoundPlayer.Ensure(rootButton, UiSoundIds.DefaultButton);
        }

        SetEmpty();
    }

    // 파괴 시 버튼 리스너를 정리합니다.
    private void OnDestroy()
    {
        if (rootButton != null)
            rootButton.onClick.RemoveListener(HandleSlotClick);
    }

    // 남은 쿨타임 비율과 숫자를 슬롯에 표시합니다.
    public void UpdateCooldown(float fillAmount, float remainTime)
    {
        bool onCooldown = fillAmount > 0;
        if (cooldownFill != null)
        {
            cooldownFill.gameObject.SetActive(onCooldown);
            cooldownFill.fillAmount = fillAmount;
        }

        if (cooldownText == null)
            return;

        if (onCooldown)
            cooldownText.SetText("{0:1}", remainTime);
        else
            cooldownText.SetText("");
    }

    // 슬롯에 스킬 정보 전체를 한 번에 반영합니다.
    public void SetSkillDisplay(int skillId, Sprite icon, int level, int openGemCount, Sprite[] gemIcons = null)
    {
        currentSkillId = skillId;
        UpdateIcon(icon);
        SetLevel(level);
        SetGemSlots(openGemCount);
        SetGemIcons(gemIcons);
    }

    private void SetGemIcons(Sprite[] gemIcons)
    {
        if (gemSlotImages == null) return;

        for (int i = 0; i < gemSlotImages.Length; i++)
        {
            if (gemSlotImages[i] == null) continue;

            Sprite icon = gemIcons != null && i < gemIcons.Length ? gemIcons[i] : null;
            gemSlotImages[i].sprite = icon;
            gemSlotImages[i].color = Color.white;
            gemSlotImages[i].preserveAspect = true;
            gemSlotImages[i].gameObject.SetActive(icon != null);
        }
    }
    // 슬롯 아이콘 이미지만 교체합니다.
    public void UpdateIcon(Sprite icon)
    {
        if (skillIcon == null)
            return;

        if (icon != null)
        {
            skillIcon.enabled = true;
            skillIcon.sprite = icon;
            skillIcon.color = Color.white;
            return;
        }

        skillIcon.sprite = emptySprite;
        skillIcon.color = defaultColor;
        skillIcon.enabled = emptySprite != null;
    }

    // 슬롯을 완전히 빈 상태로 초기화합니다.
    public void SetEmpty()
    {
        currentSkillId = -1;
        UpdateIcon(null);
        UpdateCooldown(0f, 0f);
        SetLevel(0);
        SetGemSlots(0);
    }

    // 슬롯 클릭 시 호출할 외부 콜백을 등록합니다.
    public void SetClickListener(Action callback)
    {
        clickListener = callback;
    }

    // 슬롯 버튼을 특정 강조 색상으로 덮어씁니다.
    public void SetButtonColor(Color color)
    {
        if (rootButton == null)
            return;

        ColorBlock colors = rootButton.colors;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.selectedColor = color;
        rootButton.colors = colors;
    }

    // 슬롯 버튼 색상을 초기 기본값으로 되돌립니다.
    public void ResetButtonColor()
    {
        if (rootButton != null)
            rootButton.colors = defaultButtonColors;
    }

    // 버튼 클릭을 외부 콜백으로 전달합니다.    
    private void HandleSlotClick()
    {
        clickListener?.Invoke();
    }

    // 슬롯 레벨 텍스트를 설정합니다.
    private void SetLevel(int level)
    {
        if (levelLabel == null)
            return;

        levelLabel.text = level > 0 ? $"Lv.{level}" : string.Empty;
    }

    // 열린 젬 슬롯 개수만큼 아이콘을 노출합니다.
    private void SetGemSlots(int openGemCount)
    {
        if (gemPanelRoot == null)
            return;

        gemPanelRoot.gameObject.SetActive(openGemCount > 0);

        for (int i = 0; i < gemPanelRoot.childCount; i++)
            gemPanelRoot.GetChild(i).gameObject.SetActive(i < openGemCount);
    }

}
