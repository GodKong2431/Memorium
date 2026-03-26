using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 선택한 스킬의 상세 정보를 패널에 채우고 탭 전환을 관리합니다.
/// </summary>
public sealed class SkillInfoPanelUI : MonoBehaviour, IPointerClickHandler
{
    private static readonly SkillGrade[] GradeInfoOrder =
    {
        SkillGrade.Common,
        SkillGrade.Rare,
        SkillGrade.Epic,
        SkillGrade.Legendary,
        SkillGrade.Mythic
    };

    [Header("Panels")]
    // 상세 패널을 시트 밖 오버레이로 올릴 최상위 캔버스 루트입니다.
    [SerializeField] private RectTransform overlayRoot;
    // 바깥 배경 클릭 판정에서 제외할 카드 루트입니다.
    [SerializeField] private RectTransform skillCardRoot;
    // 기본 정보를 보여주는 패널입니다.
    [SerializeField] private RectTransform defaultInfoPanel;
    // 등급 정보를 보여주는 패널입니다.
    [SerializeField] private RectTransform rarityInfoPanel;
    // 젬 선택 서브 패널입니다.
    [SerializeField] private RectTransform gemSelectRoot;

    [Header("Tabs")]
    // 기본 정보 탭 버튼입니다.
    [SerializeField] private Button defaultInfoButton;
    // 등급 정보 탭 버튼입니다.
    [SerializeField] private Button rarityInfoButton;

    [Header("Header")]
    // 선택한 스킬의 아이콘 이미지입니다.
    [SerializeField] private Image skillIconImage;
    // 선택한 스킬의 이름 텍스트입니다.
    [SerializeField] private TMP_Text skillNameText;
    // 현재 보유 등급을 표시하는 텍스트입니다.
    [SerializeField] private TMP_Text rarityText;
    // 현재 보유 개수를 표시하는 텍스트입니다.
    [SerializeField] private TMP_Text amountText;

    [Header("Default Info")]
    // 현재 스킬 레벨 텍스트입니다.
    [SerializeField] private TMP_Text currentLevelText;
    // 최대 스킬 레벨 텍스트입니다.
    [SerializeField] private TMP_Text maxLevelText;
    // 마나 소모량 텍스트입니다.
    [SerializeField] private TMP_Text manaCostText;
    // 쿨타임 텍스트입니다.
    [SerializeField] private TMP_Text cooldownText;
    // 기본 설명 텍스트입니다.
    [SerializeField] private TMP_Text defaultDescriptionText;

    [Header("Level Up")]
    // 레벨업 실행 버튼입니다.
    [SerializeField] private Button levelUpButton;
    // 레벨업 비용을 표시할 텍스트입니다.
    [SerializeField] private TMP_Text levelUpCostText;

    [Header("Gem")]
    // 열린 젬 슬롯 개수만큼 보여줄 버튼 배열입니다.
    [SerializeField] private Button[] gemButtons;
    [SerializeField] private GameObject[] gemAddObjects;
    [SerializeField] private GameObject[] gemLockObjects;
    [SerializeField] private GameObject[] gemEquipObjects;
    [SerializeField] private RectTransform gemListRoot;   
    [SerializeField] private GameObject gemItemPrefab;
    [SerializeField] private Canvas gemOverlayCanvas;
    [SerializeField] private RectTransform gemSelectAnchor;
    int selectedGemSlotIndex;

    //젬슬롯 이미지 캐싱
    private Image[] gemButtonImages;


    [Header("Rarity Info")]
    // 등급 정보 카드별 설명 텍스트 배열입니다.
    [SerializeField] private TMP_Text[] rarityDescriptionTexts;

    // 버튼 리스너를 한 번만 연결했는지 여부입니다.
    private bool isBound;
    // 현재 패널의 RectTransform 캐시입니다.
    private RectTransform cachedRectTransform;
    // 현재 패널에 표시 중인 스킬 ID입니다.
    private int currentSkillId = -1;
    // 현재 기본 정보 탭을 보고 있는지 여부입니다.
    private bool showingDefaultInfo = true;

    // 최초 로드 시 버튼 리스너와 기본 탭 상태를 준비합니다.
    private void Awake()
    {
        EnsureSetup();
        EnsureOverlayParent();
        BindButtonsOnce();
        CacheGemButtonImages();
        ApplyTabState(true);
        HideGemSelect();
    }
    private void CacheGemButtonImages()
    {
        if (gemButtons == null) return;

        gemButtonImages = new Image[gemEquipObjects.Length];
        for (int i = 0; i < gemEquipObjects.Length; i++)
        {
            if (gemEquipObjects[i] != null)
                gemButtonImages[i] = gemEquipObjects[i].GetComponent<Image>();
        }
    }
    // 현재 선택한 스킬 정보를 패널에 표시합니다.
    public void Show(int skillId)
    {
        if (!TryGetSkillState(skillId, out SkillInfoTable table, out OwnedSkillData ownedData))
            return;

        EnsureSetup();
        EnsureOverlayParent();
        BindButtonsOnce();
        currentSkillId = skillId;
        showingDefaultInfo = true;
        ApplyView(table, ownedData);
        ApplyTabState(showingDefaultInfo);
        HideGemSelect();

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }

    // 현재 표시 중인 스킬 정보를 다시 갱신합니다.
    public void RefreshCurrentSkill()
    {
        if (currentSkillId < 0)
            return;

        EnsureSetup();
        if (!TryGetSkillState(currentSkillId, out SkillInfoTable table, out OwnedSkillData ownedData))
            return;

        ApplyView(table, ownedData);
        ApplyTabState(showingDefaultInfo);
    }

    // 상세 패널을 닫고 내부 임시 상태를 정리합니다.
    public void Hide()
    {
        HideGemSelect();
        currentSkillId = -1;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    // 배경을 눌렀을 때만 상세 패널을 닫습니다.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (skillCardRoot != null &&
            RectTransformUtility.RectangleContainsScreenPoint(skillCardRoot, eventData.position, eventData.pressEventCamera))
        {
            return;
        }

        Hide();
    }

    // 탭 버튼 리스너를 한 번만 연결합니다.
    private void BindButtonsOnce()
    {
        if (isBound)
            return;

        if (defaultInfoButton != null)
        {
            defaultInfoButton.onClick.RemoveListener(HandleDefaultInfoButtonClicked);
            defaultInfoButton.onClick.AddListener(HandleDefaultInfoButtonClicked);
            UiButtonSoundPlayer.Ensure(defaultInfoButton, UiSoundIds.DefaultButton);
        }

        if (rarityInfoButton != null)
        {
            rarityInfoButton.onClick.RemoveListener(HandleRarityInfoButtonClicked);
            rarityInfoButton.onClick.AddListener(HandleRarityInfoButtonClicked);
            UiButtonSoundPlayer.Ensure(rarityInfoButton, UiSoundIds.DefaultButton);
        }
        if (levelUpButton != null)
        {
            levelUpButton.onClick.RemoveListener(HandleLevelUpButtonClicked);
            levelUpButton.onClick.AddListener(HandleLevelUpButtonClicked);
            UiButtonSoundPlayer.Ensure(levelUpButton, UiSoundIds.DefaultButton);
        }
        if (gemButtons != null)
        {
            for (int i = 0; i < gemButtons.Length; i++)
            {
                Button gemButton = gemButtons[i];
                if (gemButton == null)
                    continue;

                int capturedIndex = i;
                gemButton.onClick.RemoveAllListeners();
                gemButton.onClick.AddListener(() => HandleGemButtonClicked(capturedIndex));
                UiButtonSoundPlayer.Ensure(gemButton, UiSoundIds.DefaultButton);
            }
        }
        isBound = true;
    }

    // 패널에서 자주 쓰는 캐시와 리스너 상태를 준비합니다.
    private void EnsureSetup()
    {
        if (cachedRectTransform == null)
            cachedRectTransform = transform as RectTransform;
    }

    // 상세 패널을 시트 마스크 밖의 캔버스 오버레이 계층으로 올립니다.
    private void EnsureOverlayParent()
    {
        if (overlayRoot == null || cachedRectTransform == null)
            return;

        if (cachedRectTransform.parent != overlayRoot)
            cachedRectTransform.SetParent(overlayRoot, false);

        cachedRectTransform.anchorMin = Vector2.zero;
        cachedRectTransform.anchorMax = Vector2.one;
        cachedRectTransform.anchoredPosition = Vector2.zero;
        cachedRectTransform.sizeDelta = Vector2.zero;
        cachedRectTransform.pivot = new Vector2(0.5f, 0.5f);
        cachedRectTransform.SetAsLastSibling();
    }

    // 선택한 스킬 데이터로 패널의 모든 표시값을 갱신합니다.
    private void ApplyView(SkillInfoTable table, OwnedSkillData ownedData)
    {
        ApplyHeader(table, ownedData);
        ApplyDefaultInfo(table, ownedData);
        ApplyLevelUpButton(ownedData); 
        ApplyGemButtons(ownedData);
        ApplyRarityDescriptions(ownedData);
    }

    // 상단 아이콘, 이름, 등급, 보유 수량을 채웁니다.
    private void ApplyHeader(SkillInfoTable table, OwnedSkillData ownedData)
    {
        if (skillIconImage != null)
        {
            Sprite icon = SkillIconResolver.TryLoad(table.skillIcon, currentSkillId);
            skillIconImage.sprite = icon;
            skillIconImage.color = icon != null ? Color.white : new Color(1f, 1f, 1f, 0.35f);
        }

        if (skillNameText != null)
            skillNameText.SetText(string.IsNullOrWhiteSpace(table.skillName) ? $"Skill {currentSkillId}" : table.skillName);

        if (rarityText != null)
            rarityText.SetText(GetGradeDisplayName(GetCurrentGrade(ownedData)));

        if (amountText != null)
            amountText.SetText("x {0}", GetCurrentCount(ownedData));
    }

    // 기본 정보 탭의 레벨, 마나, 쿨타임, 설명을 채웁니다.
    private void ApplyDefaultInfo(SkillInfoTable table, OwnedSkillData ownedData)
    {
        int level = ownedData != null ? ownedData.level : 0;
        Debug.Log($"[SkillInfoPanel] skillId={currentSkillId} level={level} maxLevel={ownedData?.MaxLevel}");

        SetNumberText(currentLevelText, ownedData != null ? ownedData.level : 0);
        SetNumberText(maxLevelText, ownedData != null ? ownedData.MaxLevel : 0);

        if (manaCostText != null)
            manaCostText.SetText(FormatValue(table.manaCost));

        if (cooldownText != null)
            cooldownText.SetText($"{FormatValue(table.skillCooldown)}s");

        if (defaultDescriptionText != null)
            defaultDescriptionText.SetText(GetDescription(table));
    }
    private void ApplyLevelUpButton(OwnedSkillData ownedData)
    {
        SkillInventoryModule skillModule = GetSkillModule();

        bool showLevelUp = ownedData != null && ownedData.IsEquippable;

        if (levelUpButton != null)
            levelUpButton.gameObject.SetActive(showLevelUp);

        if (levelUpCostText != null)
            levelUpCostText.gameObject.SetActive(showLevelUp);

        if (!showLevelUp)
            return;


        if (levelUpButton != null)
        {
            bool canLevelUp = skillModule != null
                && skillModule.CanLevelUpSkill(currentSkillId);
            levelUpButton.interactable = canLevelUp;
        }
    }
    private void HandleLevelUpButtonClicked()
    {
        if (currentSkillId < 0)
            return;

        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        if (!skillModule.TryLevelUpSkill(currentSkillId))
            return;

        RefreshCurrentSkill();
    }

    // 열린 젬 슬롯 개수만큼 버튼을 표시합니다.
    private void ApplyGemButtons(OwnedSkillData ownedData)
    {
        if (gemButtons == null)
            return;

        for (int i = 0; i < gemButtons.Length; i++)
        {
            Button gemButton = gemButtons[i];
            if (gemButton == null)
                continue;

            bool isOpen = IsGemSlotOpen(ownedData, i);
            int equippedGemId = GetEquippedGemId(currentSkillId, i);
            bool hasEquippedGem = equippedGemId > 0;

            gemButton.gameObject.SetActive(true);
            gemButton.interactable = isOpen;
            SetGemObjectState(gemLockObjects, i, !isOpen);
            SetGemObjectState(gemAddObjects, i, isOpen && !hasEquippedGem);
            SetGemObjectState(gemEquipObjects, i, hasEquippedGem);

            if (hasEquippedGem && gemButtonImages != null && i < gemButtonImages.Length && gemButtonImages[i] != null)
            {
                Sprite icon = GetGemIcon(equippedGemId);
                gemButtonImages[i].sprite = icon;
            }
        }
    }
    private void HandleGemButtonClicked(int gemSlotIndex)
    {
        if (currentSkillId < 0)
            return;

        if (!TryGetSkillState(currentSkillId, out _, out OwnedSkillData ownedData))
            return;

        if (!IsGemSlotOpen(ownedData, gemSlotIndex))
            return;

        selectedGemSlotIndex = gemSlotIndex;
        PositionGemSelect(gemSlotIndex);
        ShowGemSelect();
    }
    private void PositionGemSelect(int gemSlotIndex)
    {

        if (gemSelectAnchor == null || gemButtons == null)
            return;

        if (gemSlotIndex < 0 || gemSlotIndex >= gemButtons.Length || gemButtons[gemSlotIndex] == null)
            return;

        RectTransform buttonRect = gemButtons[gemSlotIndex].GetComponent<RectTransform>();
        if (buttonRect == null)
            return;

        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(null, buttonRect.position);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            gemSelectAnchor.parent as RectTransform,
            screenPos,
            null,
            out Vector2 localPos
        );

        Vector2 pos = gemSelectAnchor.anchoredPosition;
        pos.x = localPos.x;
        gemSelectAnchor.anchoredPosition = pos;
    }
    private void ShowGemSelect()
    {
        if (gemSelectRoot == null)
            return;

        if (gemOverlayCanvas != null)
        {
            gemOverlayCanvas.overrideSorting = true;
            gemOverlayCanvas.sortingOrder = 100; 
        }

        gemSelectRoot.gameObject.SetActive(true);
        PopulateGemList();
    }
    private void PopulateGemList()
    {
        if (gemListRoot == null || gemItemPrefab == null)
            return;

        for (int i = gemListRoot.childCount - 1; i >= 0; i--)
            Destroy(gemListRoot.GetChild(i).gameObject);

        var gemModule = InventoryManager.Instance.GetModule<GemInventoryModule>();
        if (gemModule == null)
            return;

        List<GemDisplayData> gemList;
        if (selectedGemSlotIndex == 2)
            gemList = gemModule.GetEquippableM4GemDisplayList(currentSkillId);
        else
            gemList = gemModule.GetEquippableM5GemDisplayList(currentSkillId);

        foreach (var gem in gemList)
        {
            GameObject obj = Instantiate(gemItemPrefab, gemListRoot);
            GemItemView view = obj.GetComponent<GemItemView>();
            if (view == null)
                continue;

            int gemId = gem.GemId;
            Sprite icon = GetGemIcon(gemId);
            int count = gem.HighestGradeCount;

            view.Bind(gemId, icon, count, OnGemSelected);
        }
    }
    private void OnGemSelected(int gemId)
    {
        var gemModule = InventoryManager.Instance.GetModule<GemInventoryModule>();
        if (gemModule == null)
            return;

        if (selectedGemSlotIndex == 2)
        {
            gemModule.TryEquipM4GemBySkillId(currentSkillId, gemId);
        }
        else
        {
            gemModule.TryEquipM5GemBySkillId(currentSkillId, selectedGemSlotIndex, gemId);
        }

        HideGemSelect();
        RefreshCurrentSkill();
    }

    private Sprite GetGemIcon(int itemId)
    {
        if (itemId <= 0) return null;

        var gemModule = InventoryManager.Instance?.GetModule<GemInventoryModule>();
        if (gemModule == null) return null;

        int m4Id = gemModule.GetM4IdByItemId(itemId);
        if (m4Id != 0 && DataManager.Instance.SkillModule4Dict.TryGetValue(m4Id, out var m4Data))
        {
            Sprite sprite = SkillIconResolver.TryLoad(m4Data.m4Icon);
            if (sprite != null) return sprite;
        }

        int m5Id = gemModule.GetM5IdByItemId(itemId);

        if (m5Id != 0 && DataManager.Instance.SkillModule5Dict.TryGetValue(m5Id, out var m5Data))
        {
            Sprite sprite = SkillIconResolver.TryLoad(m5Data.m5Icon);
            if (sprite != null) return sprite;
        }

        return null;
    }
    // 등급 정보 탭의 설명 문구를 현재 보유 등급 기준으로 채웁니다.
    private void ApplyRarityDescriptions(OwnedSkillData ownedData)
    {
        if (rarityDescriptionTexts == null)
            return;

        SkillGrade currentGrade = GetCurrentGrade(ownedData);
        for (int i = 0; i < rarityDescriptionTexts.Length && i < GradeInfoOrder.Length; i++)
        {
            TMP_Text label = rarityDescriptionTexts[i];
            if (label == null)
                continue;

            label.SetText(GetRarityDescription(GradeInfoOrder[i], currentGrade));
        }
    }

    // 기본 정보 탭을 표시합니다.
    private void HandleDefaultInfoButtonClicked()
    {
        showingDefaultInfo = true;
        ApplyTabState(showingDefaultInfo);
    }

    // 등급 정보 탭을 표시합니다.
    private void HandleRarityInfoButtonClicked()
    {
        showingDefaultInfo = false;
        ApplyTabState(showingDefaultInfo);
    }

    // 현재 선택된 탭에 맞게 패널 활성 상태와 버튼 상태를 갱신합니다.
    private void ApplyTabState(bool showDefaultInfo)
    {
        if (defaultInfoPanel != null)
            defaultInfoPanel.gameObject.SetActive(showDefaultInfo);

        if (rarityInfoPanel != null)
            rarityInfoPanel.gameObject.SetActive(!showDefaultInfo);

        if (defaultInfoButton != null)
            defaultInfoButton.interactable = !showDefaultInfo;

        if (rarityInfoButton != null)
            rarityInfoButton.interactable = showDefaultInfo;
    }

    // 선택 패널이 남아 있으면 상세 패널을 열 때 함께 닫습니다.
    private void HideGemSelect()
    {
        if (gemSelectRoot != null)
            gemSelectRoot.gameObject.SetActive(false);

        if (gemOverlayCanvas != null)
            gemOverlayCanvas.overrideSorting = false;

    }

    // 스킬 테이블과 보유 데이터를 함께 가져옵니다.
    private static bool TryGetSkillState(int skillId, out SkillInfoTable table, out OwnedSkillData ownedData)
    {
        table = null;
        ownedData = null;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.SkillInfoDict == null)
            return false;

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out table))
            return false;

        SkillInventoryModule skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        ownedData = skillModule != null ? skillModule.GetSkillData(skillId) : null;
        return true;
    }

    // 보유 데이터 기준 현재 표시할 등급을 반환합니다.
    private static SkillGrade GetCurrentGrade(OwnedSkillData ownedData)
    {
        //return ownedData != null ? ownedData.HighestGrade : SkillGrade.Scroll;
        return ownedData != null ? ownedData.GetGrade() : SkillGrade.Scroll;
    }

    // 보유 데이터 기준 현재 표시할 수량을 반환합니다.
    private static int GetCurrentCount(OwnedSkillData ownedData)
    {
        //if (ownedData == null)
        //    return 0;

        //return ownedData.IsEquippable
        //    ? ownedData.OwnedScollCount
        //    : ownedData.GetCount(SkillGrade.Scroll);

        if (ownedData == null)
            return 0;

        return (int)ownedData.GetOwnedScrollCount().ToDouble();
    }

    // 열린 젬 슬롯 수를 보유 데이터에서 계산합니다.
    private static bool IsGemSlotOpen(OwnedSkillData ownedData, int gemSlotIndex)
    {
        if (ownedData == null)
            return false;

        switch (gemSlotIndex)
        {
            case 0:
                return ownedData.IsM5JemSlotOpen(0);
            case 1:
                return ownedData.IsM5JemSlotOpen(1);
            case 2:
                return ownedData.IsM4JemSlotOpen;
            default:
                return false;
        }
    }

    private static int GetEquippedGemId(int skillId, int gemSlotIndex)
    {
        SkillPresetSlot presetSlot = GetCurrentPresetSlot(skillId);
        if (presetSlot == null)
            return -1;

        switch (gemSlotIndex)
        {
            case 0:
            case 1:
                return presetSlot.m5JemIDs != null && gemSlotIndex < presetSlot.m5JemIDs.Length
                    ? presetSlot.m5JemIDs[gemSlotIndex]
                    : -1;
            case 2:
                return presetSlot.m4JemID;
            default:
                return -1;
        }
    }
    private static SkillPresetSlot GetCurrentPresetSlot(int skillId)
    {
        SkillInventoryModule skillModule = InventoryManager.Instance != null
         ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
         : null;
        SkillGemSlotData gemData = skillModule?.GetGemSlotData(skillId);

        if (gemData == null)
            return null;
        return new SkillPresetSlot(gemData.skillID, gemData.m5JemIDs, gemData.m4JemID);
    }

    private static SkillInventoryModule GetSkillModule()
    {
        return InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
    }

    private static void SetGemObjectState(GameObject[] objects, int index, bool isActive)
    {
        if (objects == null || index < 0 || index >= objects.Length || objects[index] == null)
            return;

        objects[index].SetActive(isActive);
    }

    // 스킬 설명 필드 중 실제 값이 있는 문자열을 우선 사용합니다.
    private static string GetDescription(SkillInfoTable table)
    {
        if (!string.IsNullOrWhiteSpace(table.skillDesc))
            return table.skillDesc;

        return string.IsNullOrWhiteSpace(table.desc) ? string.Empty : table.desc;
    }

    // 등급 enum 값을 한국어 표시 문자열로 변환합니다.
    private static string GetGradeDisplayName(SkillGrade grade)
    {
        switch (grade)
        {
            case SkillGrade.Scroll:
                return "스크롤";
            case SkillGrade.Common:
                return "일반";
            case SkillGrade.Rare:
                return "희귀";
            case SkillGrade.Epic:
                return "영웅";
            case SkillGrade.Legendary:
                return "전설";
            case SkillGrade.Mythic:
                return "신화";
            default:
                return grade.ToString();
        }
    }

    // 등급 카드에 표시할 설명 문구를 만듭니다.
    private static string GetRarityDescription(SkillGrade grade, SkillGrade currentGrade)
    {
        string prefix = grade == currentGrade ? "현재 등급 / " : string.Empty;
        switch (grade)
        {
            case SkillGrade.Common:
                return $"{prefix} 장착 가능";
            case SkillGrade.Rare:
                return $"{prefix} 젬 슬롯 1 해금";
            case SkillGrade.Epic:
                return $"{prefix} 젬 슬롯 2 해금";
            case SkillGrade.Legendary:
                return $"{prefix} 젬 슬롯 3 해금";
            case SkillGrade.Mythic:
                return $"{prefix} 대미지 50% 증가";
            default:
                return string.Empty;
        }
    }

    // 숫자 텍스트를 공통 형식으로 설정합니다.
    private static void SetNumberText(TMP_Text label, int value)
    {
        if (label != null)
            label.SetText(value.ToString());
    }

    // 소수점이 필요할 때만 남기는 숫자 문자열을 반환합니다.
    private static string FormatValue(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.0");
    }
}
