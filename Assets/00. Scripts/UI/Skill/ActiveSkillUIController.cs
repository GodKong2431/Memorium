using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 액티브 스킬 목록, 프리셋, 장착 슬롯 UI를 한 곳에서 갱신하는 컨트롤러입니다.
/// </summary>
public class ActiveSkillUIController : UIControllerBase
{
    // 스킬 잠금 해제와 승급에 필요한 기본 조각 수입니다.
    private const int RequiredMergeCount = 3;

    [Serializable]
    /// <summary>
    /// 스킬 상세보기 요청을 외부로 전달하는 이벤트입니다.
    /// </summary>
    public sealed class SkillDetailRequestedEvent : UnityEvent<int>
    {
    }

    [Header("List")]
    // 스킬 아이템이 생성될 스크롤 콘텐츠 루트입니다.
    [SerializeField] private RectTransform listRoot;
    // 스킬 아이템 UI 프리팹입니다.
    [SerializeField] private GameObject itemPrefab;
    // 목록 재구성 전에 기존 아이템을 지울지 여부입니다.
    [SerializeField] private bool clearListOnBuild = true;

    [Header("Preset")]
    // 프리셋 전환 버튼 배열입니다.
    [SerializeField] private Button[] presetButtons;

    [Header("Equipped Slots")]
    // 현재 프리셋의 장착 슬롯 UI 배열입니다.
    [SerializeField] private BattleSkillSlotView[] equippedSkillSlots;
    // 장착 대기 상태일 때만 표시할 슬롯 선택 패널입니다.
    [SerializeField] private GameObject equipSkillPanelRoot;

    [Header("Detail")]
    // 스킬 상세 정보를 표시할 패널입니다.
    [SerializeField] private SkillInfoPanelUI skillInfoPanel;

    [Header("Colors")]
    // 비활성 프리셋 버튼 기본 색상입니다.
    [SerializeField] private Color presetNormalColor = Color.white;
    // 선택된 프리셋 버튼 강조 색상입니다.
    [SerializeField] private Color presetSelectedColor = new Color32(135, 206, 235, 255);
    // 장착 가능한 슬롯 표시 색상입니다.
    [SerializeField] private Color slotSelectableColor = new Color32(255, 215, 64, 255);
    // 장착할 수 없는 슬롯 표시 색상입니다.
    [SerializeField] private Color slotBlockedColor = new Color32(170, 170, 170, 255);

    [Header("Events")]
    // 스킬 아이콘 클릭 시 상세보기로 넘길 이벤트입니다.
    [SerializeField] private SkillDetailRequestedEvent onSkillDetailRequested = new SkillDetailRequestedEvent();

    [Header("Equip Selection")]
    //오버레이 캔버스
    [SerializeField] private Canvas equipOverlayCanvas;
    //포인터 이미지 0,1 일반스킬 2 궁극
    [SerializeField] private GameObject[] equipPointerObjects;
    //스킬 이미지
    [SerializeField] private Image equipSkillIconImage;
    //스킬 이름
    [SerializeField] private TMP_Text equipSkillNameText;


    // 생성된 스킬 아이템 뷰를 스킬 ID 기준으로 캐시합니다.
    private readonly Dictionary<int, ActiveSkillItemView> itemViews = new Dictionary<int, ActiveSkillItemView>();
    // 로드한 아이콘을 재사용하기 위한 캐시입니다.
    private readonly Dictionary<int, Sprite> iconCache = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, Sprite> gemIconCache = new Dictionary<int, Sprite>();

    // 현재 구독 중인 스킬 인벤토리 모듈입니다.
    private SkillInventoryModule subscribedSkillModule;
    // 데이터 준비를 기다리는 코루틴 핸들입니다.
    private Coroutine waitReadyRoutine;
    // 목록이 한 번이라도 생성되었는지 여부입니다.
    private bool built;
    // 현재 장착 대기 중인 스킬 ID입니다.
    private int pendingEquipSkillId = -1;
    // 현재 장착 대기 중인 스킬 타입입니다.
    private SkillType pendingEquipSkillType;

    // 마지막으로 상세보기 요청한 스킬 ID입니다.
    public int CurrentDetailSkillId { get; private set; } = -1;

    // UI 활성 시 버튼과 모듈 이벤트를 연결합니다.
    protected override void Subscribe()
    {
        EnsureSkillModuleSubscribed();
        BindPresetButtons();
        BindEquippedSkillSlots();
    }

    // UI 비활성 시 버튼과 모듈 이벤트를 해제합니다.
    protected override void Unsubscribe()
    {
        UnbindPresetButtons();
        UnbindEquippedSkillSlots();
        UnsubscribeFromSkillModule();
    }

    // 현재 데이터 상태를 기준으로 전체 화면을 다시 그립니다.
    protected override void RefreshView()
    {
        EnsureSkillModuleSubscribed();

        if (!IsReady())
            return;

        BuildIfNeeded();
        RebuildAll();
    }

    // 활성화될 때 데이터 준비 대기 루틴을 시작합니다.
    protected override void OnEnable()
    {
        base.OnEnable();
        StartWaitReadyRoutine();
    }

    // 비활성화될 때 대기 루틴을 중지합니다.
    protected override void OnDisable()
    {
        if (waitReadyRoutine != null)
        {
            StopCoroutine(waitReadyRoutine);
            waitReadyRoutine = null;
        }

        if (skillInfoPanel != null)
            skillInfoPanel.Hide();

        base.OnDisable();
    }

    // 스킬 목록을 아직 만들지 않았다면 한 번만 생성합니다.
    private void BuildIfNeeded()
    {
        if (built)
            return;

        List<int> skillIds = GetDisplaySkillIds();
        if (skillIds.Count == 0)
            return;

        if (clearListOnBuild)
            ClearList();

        itemViews.Clear();

        for (int i = 0; i < skillIds.Count; i++)
        {
            GameObject itemObject = Instantiate(itemPrefab, listRoot, false);
            itemObject.name = $"ActiveSkillItem_{skillIds[i]}";

            ActiveSkillItemBinding binding = itemObject.GetComponent<ActiveSkillItemBinding>();
            if (binding == null)
            {
                Debug.LogWarning($"[ActiveSkillUIController] ActiveSkillItemBinding missing: {itemObject.name}");
                continue;
            }

            itemViews[skillIds[i]] = new ActiveSkillItemView(binding);
        }

        built = itemViews.Count > 0;
    }

    // 스크롤 콘텐츠 아래의 기존 스킬 아이템을 모두 제거합니다.
    private void ClearList()
    {
        if (listRoot == null)
            return;

        for (int i = listRoot.childCount - 1; i >= 0; i--)
            Destroy(listRoot.GetChild(i).gameObject);
    }

    // 프리셋, 목록, 장착 슬롯 상태를 한 번에 갱신합니다.
    private void RebuildAll()
    {
        RebuildPresetButtons();
        RebuildSkillItems();    
        RebuildEquippedSkills();
        UpdateEquipSelectionState();
    }

    // 각 스킬 아이템에 현재 소유 상태를 반영합니다.
    private void RebuildSkillItems()
    {
        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return;

        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        foreach (KeyValuePair<int, ActiveSkillItemView> pair in itemViews)
        {
            if (!DataManager.Instance.SkillInfoDict.TryGetValue(pair.Key, out SkillInfoTable table))
                continue;

            OwnedSkillData ownedData = skillModule.GetSkillData(pair.Key);
            Sprite icon = GetSkillIcon(pair.Key, table.skillIcon);
            int level = ownedData != null ? ownedData.level : 0;
            int openGemCount = GetOpenGemCount(ownedData);
            bool canEquip = ownedData != null && ownedData.IsEquippable;
            int currentCount;
            if (ownedData != null)
            {
                currentCount = Mathf.FloorToInt(ownedData.GetOwnedScrollCount().ToFloat());
            }
            else
            {
                int scrollItemId = table.skillScrollID;
                currentCount = scrollItemId > 0
                    ? Mathf.FloorToInt(InventoryManager.Instance.GetItemAmount(scrollItemId).ToFloat())
                    : 0;
            }
            ActiveSkillItemVisualState visualState = GetVisualState(ownedData, currentCount);
            bool canTriggerStateAction = CanTriggerStateAction(ownedData, visualState, currentCount);
            ActiveSkillItemGemSlotDisplayData[] upgradeGemSlots = BuildUpgradeGemSlots(pair.Key, ownedData);
            bool canLevelUp = skillModule.CanLevelUpSkill(pair.Key);
            string levelUpCostString;
            if (ownedData != null)
            {
                int owned = Mathf.FloorToInt(ownedData.GetOwnedScrollCount().ToFloat());
                if (ownedData.CanLevelUp)
                {
                    int cost = Mathf.FloorToInt(ownedData.GetLevelUpCost().ToFloat());
                    levelUpCostString = $"{owned}/{cost}";
                }
                else
                {
                    levelUpCostString = $"{owned}/MAX";
                }
            }
            else
            {
                levelUpCostString = "0/MAX";
            }

            pair.Value.Bind(
                new ActiveSkillItemDisplayData(
                    pair.Key,
                    table.skillName,
                    icon,
                    level,
                    openGemCount,
                    canEquip,
                    visualState,
                    currentCount,
                    RequiredMergeCount,
                    canTriggerStateAction,
                    upgradeGemSlots,
                    canLevelUp, 
                    levelUpCostString),
                HandleSkillDetailRequested,
                BeginEquipSelection,
                HandleItemStateActionClicked,
                HandleLevelUpClicked);
        }
    }

    // 현재 프리셋 기준으로 장착 슬롯 UI를 다시 그립니다.
    private void RebuildEquippedSkills()
    {
        if (equippedSkillSlots == null || equippedSkillSlots.Length == 0)
            return;

        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        SkillPreset preset = skillModule.GetCurrentPresetSnapshot();
        for (int i = 0; i < equippedSkillSlots.Length; i++)
        {
            BattleSkillSlotView slotView = equippedSkillSlots[i];
            if (slotView == null)
                continue;

            if (preset == null || i >= preset.slots.Length || ShouldDisplayAsEmpty(preset.slots[i]))
            {
                slotView.SetEmpty();
                continue;
            }

            int skillId = preset.slots[i].skillID;
            DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out SkillInfoTable table);
            OwnedSkillData ownedData = skillModule.GetSkillData(skillId);
            Sprite icon = table != null ? GetSkillIcon(skillId, table.skillIcon) : null;
            slotView.SetSkillDisplay(skillId, icon, ownedData != null ? ownedData.level : 0, GetOpenGemCount(ownedData));
        }
    }

    // 프리셋 버튼 클릭 이벤트를 연결합니다.
    private void BindPresetButtons()
    {
        if (presetButtons == null)
            return;

        for (int i = 0; i < presetButtons.Length; i++)
        {
            Button button = presetButtons[i];
            if (button == null)
                continue;

            int capturedIndex = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnPresetButtonClicked(capturedIndex));
        }
    }

    // 프리셋 버튼 클릭 이벤트를 해제합니다.
    private void UnbindPresetButtons()
    {
        if (presetButtons == null)
            return;

        for (int i = 0; i < presetButtons.Length; i++)
        {
            if (presetButtons[i] != null)
                presetButtons[i].onClick.RemoveAllListeners();
        }
    }

    // 장착 슬롯 클릭 이벤트를 연결합니다.
    private void BindEquippedSkillSlots()
    {
        if (equippedSkillSlots == null)
            return;

        for (int i = 0; i < equippedSkillSlots.Length; i++)
        {
            BattleSkillSlotView slotView = equippedSkillSlots[i];
            if (slotView == null)
                continue;

            int capturedIndex = i;
            slotView.SetClickListener(() => OnEquippedSkillButtonClicked(capturedIndex));
        }
    }

    // 장착 슬롯 클릭 이벤트를 해제합니다.
    private void UnbindEquippedSkillSlots()
    {
        if (equippedSkillSlots == null)
            return;

        for (int i = 0; i < equippedSkillSlots.Length; i++)
        {
            if (equippedSkillSlots[i] != null)
                equippedSkillSlots[i].SetClickListener(null);
        }
    }

    // 프리셋 버튼 클릭 시 활성 프리셋을 전환합니다.
    private void OnPresetButtonClicked(int presetIndex)
    {
        GetSkillModule()?.SwitchPreset(presetIndex);
    }

    // 슬롯 클릭 시 상세보기 또는 장착 처리로 분기합니다.
    private void OnEquippedSkillButtonClicked(int slotIndex)
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null || slotIndex < 0 || equippedSkillSlots == null || slotIndex >= equippedSkillSlots.Length)
            return;

        if (pendingEquipSkillId >= 0)
        {
            if (!CanEquipToSlot(slotIndex, pendingEquipSkillType))
                return;

            if (!skillModule.SetPresetSlot(slotIndex, pendingEquipSkillId))
                return;

            ClearEquipSelection();
            RebuildAll();
            return;
        }

        BattleSkillSlotView slotView = equippedSkillSlots[slotIndex];
        if (slotView != null && slotView.CurrentSkillId >= 0)
            HandleSkillDetailRequested(slotView.CurrentSkillId);
    }

    // 리스트에서 선택한 스킬을 장착 대기 상태로 전환합니다.
    private void BeginEquipSelection(int skillId)
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null || DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return;

        OwnedSkillData ownedData = skillModule.GetSkillData(skillId);
        if (ownedData == null || !ownedData.IsEquippable)
            return;

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out SkillInfoTable table))
            return;

        pendingEquipSkillId = skillId;
        pendingEquipSkillType = table.skillType;

        if (equipSkillIconImage != null)
            equipSkillIconImage.sprite = GetSkillIcon(skillId, table.skillIcon);

        if (equipSkillNameText != null)
            equipSkillNameText?.SetText(table.skillName);

        UpdateEquipSelectionState();
    }

    // 잠금 해제 또는 승급 버튼 클릭을 처리합니다.
    private void HandleItemStateActionClicked(int skillId)
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        ClearEquipSelection();
        skillModule.TryUnlockSkill(skillId);
    }

    // 장착 대기 상태를 초기화합니다.
    private void ClearEquipSelection()
    {
        pendingEquipSkillId = -1;
        pendingEquipSkillType = default;
        UpdateEquipSelectionState();
    }
    private void HandleLevelUpClicked(int skillId)
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        skillModule.TryLevelUpSkill(skillId);
        // OnInventoryChanged 이벤트로 RebuildAll 자동 호출
    }

    // 상세보기 요청 스킬 ID를 저장하고 이벤트를 발행합니다.
    private void HandleSkillDetailRequested(int skillId)
    {
        CurrentDetailSkillId = skillId;
        if (skillInfoPanel != null)
            skillInfoPanel.Show(skillId);

        onSkillDetailRequested?.Invoke(skillId);
    }

    // 장착 가능 여부에 따라 슬롯 패널과 버튼 색상을 갱신합니다.
    private void UpdateEquipSelectionState()
    {
        bool isSelecting = pendingEquipSkillId >= 0;

        if (equipSkillPanelRoot != null)
            equipSkillPanelRoot.SetActive(isSelecting);

        // 캔버스 오버레이
        if (equipOverlayCanvas != null)
        {
            equipOverlayCanvas.overrideSorting = isSelecting;
            if (isSelecting)
                equipOverlayCanvas.sortingOrder = 100;
        }

        // 포인터 이미지
        if (equipPointerObjects != null)
        {
            for (int i = 0; i < equipPointerObjects.Length; i++)
            {
                if (equipPointerObjects[i] == null)
                    continue;

                bool show = isSelecting && CanEquipToSlot(i, pendingEquipSkillType);
                equipPointerObjects[i].SetActive(show);
            }
        }
        if (equippedSkillSlots == null)
            return;

        for (int i = 0; i < equippedSkillSlots.Length; i++)
        {
            BattleSkillSlotView slotView = equippedSkillSlots[i];
            if (slotView == null)
                continue;

            if (!isSelecting)
            {
                slotView.ResetButtonColor();
                continue;
            }

            bool canEquip = CanEquipToSlot(i, pendingEquipSkillType);
            slotView.SetButtonColor(canEquip ? slotSelectableColor : slotBlockedColor);
        }
    }

    // 현재 프리셋 인덱스에 맞춰 탭 색상을 갱신합니다.
    private void RebuildPresetButtons()
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null || presetButtons == null)
            return;

        int currentPresetIndex = skillModule.CurrentPresetIndex;
        for (int i = 0; i < presetButtons.Length; i++)
        {
            Button button = presetButtons[i];
            if (button == null)
                continue;

            Color targetColor = i == currentPresetIndex ? presetSelectedColor : presetNormalColor;
            ColorBlock colors = button.colors;
            colors.normalColor = targetColor;
            colors.highlightedColor = targetColor;
            colors.selectedColor = targetColor;
            button.colors = colors;
        }
    }

    // 인벤토리 수량 변경 시 화면을 다시 그립니다.
    private void OnSkillInventoryChanged()
    {
        if (skillInfoPanel != null)
            skillInfoPanel.RefreshCurrentSkill();

        if (!built)
        {
            RefreshView();
            return;
        }

        RebuildAll();
    }

    // 프리셋 변경 시 선택 상태를 지우고 화면을 갱신합니다.
    private void OnPresetChanged(int presetIndex)
    {
        ClearEquipSelection();

        if (skillInfoPanel != null)
            skillInfoPanel.RefreshCurrentSkill();

        if (!built)
        {
            RefreshView();
            return;
        }

        RebuildAll();
    }

    // 현재 인벤토리 모듈에 한 번만 이벤트를 구독합니다.
    private void EnsureSkillModuleSubscribed()
    {
        SkillInventoryModule module = GetSkillModule();
        if (module == null || subscribedSkillModule == module)
            return;

        UnsubscribeFromSkillModule();
        subscribedSkillModule = module;
        subscribedSkillModule.OnInventoryChanged += OnSkillInventoryChanged;
        subscribedSkillModule.OnPresetChanged += OnPresetChanged;
    }

    // 이전 인벤토리 모듈 구독을 정리합니다.
    private void UnsubscribeFromSkillModule()
    {
        if (subscribedSkillModule == null)
            return;

        subscribedSkillModule.OnInventoryChanged -= OnSkillInventoryChanged;
        subscribedSkillModule.OnPresetChanged -= OnPresetChanged;
        subscribedSkillModule = null;
    }

    // 화면에 표시할 액티브 스킬 ID 목록을 정렬해서 반환합니다.
    private List<int> GetDisplaySkillIds()
    {
        List<int> skillIds = new List<int>();

        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return skillIds;

        foreach (KeyValuePair<int, SkillInfoTable> pair in DataManager.Instance.SkillInfoDict)
        {
            if (!IsEquippableSkillType(pair.Value.skillType))
                continue;

            skillIds.Add(pair.Key);
        }

        skillIds.Sort((a, b) => a.CompareTo(b));
        return skillIds;
    }

    // 현재 활성 스킬 인벤토리 모듈을 가져옵니다.
    private SkillInventoryModule GetSkillModule()
    {
        return InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
    }

    // 목록 생성과 렌더링이 가능한 초기화 상태인지 확인합니다.
    private bool IsReady()
    {
        return listRoot != null
            && itemPrefab != null
            && DataManager.Instance != null
            && DataManager.Instance.DataLoad
            && DataManager.Instance.SkillInfoDict != null
            && GetSkillModule() != null;
    }

    // 데이터 로딩 완료를 기다리는 코루틴을 시작합니다.
    private void StartWaitReadyRoutine()
    {
        if (waitReadyRoutine != null)
            StopCoroutine(waitReadyRoutine);

        waitReadyRoutine = StartCoroutine(CoWaitUntilReady());
    }

    // 필요한 매니저가 준비될 때까지 대기한 뒤 화면을 그립니다.
    private IEnumerator CoWaitUntilReady()
    {
        yield return new WaitUntil(IsReady);
        RefreshView();
        waitReadyRoutine = null;
    }

    // 스킬 ID 기준으로 아이콘을 캐시해서 반환합니다.
    private Sprite GetSkillIcon(int skillId, string iconKey)
    {
        if (!iconCache.TryGetValue(skillId, out Sprite cached))
        {
            cached = SkillIconResolver.TryLoad(iconKey, skillId);
            iconCache[skillId] = cached;
        }

        return cached;
    }

    private ActiveSkillItemGemSlotDisplayData[] BuildUpgradeGemSlots(int skillId, OwnedSkillData ownedData)
    {
        ActiveSkillItemGemSlotDisplayData[] slots = new ActiveSkillItemGemSlotDisplayData[3];
        SkillPresetSlot presetSlot = GetCurrentPresetSlot(skillId);

        for (int i = 0; i < slots.Length; i++)
        {
            bool isUnlocked = IsGemSlotOpen(ownedData, i);
            int equippedGemId = GetEquippedGemId(presetSlot, i);
            bool hasEquippedGem = equippedGemId > 0;
            Sprite gemIcon = hasEquippedGem ? GetGemIcon(equippedGemId) : null;
            slots[i] = new ActiveSkillItemGemSlotDisplayData(isUnlocked, hasEquippedGem, gemIcon);
        }

        return slots;
    }

    private Sprite GetGemIcon(int itemId)
    {
        if (itemId <= 0)
            return null;

        if (gemIconCache.TryGetValue(itemId, out Sprite cached))
            return cached;

        Sprite sprite = null;
        if (DataManager.Instance?.ItemInfoDict != null &&
            DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out ItemInfoTable itemInfo) &&
            itemInfo != null &&
            !string.IsNullOrWhiteSpace(itemInfo.itemIcon))
        {
            string key = itemInfo.itemIcon.Trim();
            sprite = Resources.Load<Sprite>(key);
            if (sprite == null)
            {
                int extensionIndex = key.LastIndexOf(".", StringComparison.Ordinal);
                if (extensionIndex > 0)
                    sprite = Resources.Load<Sprite>(key.Substring(0, extensionIndex));
            }

            if (sprite == null)
            {
                const string resourcesToken = "Resources/";
                int resourcesIndex = key.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
                if (resourcesIndex >= 0)
                {
                    string relativePath = key.Substring(resourcesIndex + resourcesToken.Length);
                    int extensionIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
                    if (extensionIndex > 0)
                        relativePath = relativePath.Substring(0, extensionIndex);

                    sprite = Resources.Load<Sprite>(relativePath);
                }
            }
        }

        gemIconCache[itemId] = sprite;
        return sprite;
    }

    // 열린 젬 슬롯 수를 소유 데이터에서 계산합니다.
    private static int GetOpenGemCount(OwnedSkillData ownedData)
    {
        if (ownedData == null)
            return 0;

        int openCount = 0;
        if (ownedData.IsM5JemSlotOpen(0))
            openCount++;
        if (ownedData.IsM5JemSlotOpen(1))
            openCount++;
        if (ownedData.IsM4JemSlotOpen)
            openCount++;

        return openCount;
    }

    // 현재 합성 가능한 조각 수를 상태에 맞게 계산합니다.
    private static int GetCurrentMergeCount(OwnedSkillData ownedData)
    {
        if (ownedData == null)
            return 0;
        //return ownedData.IsEquippable
        //    ? ownedData.OwnedScollCount
        //    : ownedData.GetCount(SkillGrade.Scroll);

        return (int)ownedData.GetOwnedScrollCount().ToFloat();
    }

    // 소유 상태와 개수에 따라 아이템 표시 상태를 결정합니다.
    private static ActiveSkillItemVisualState GetVisualState(OwnedSkillData ownedData, int currentCount)
    {
        if (ownedData != null && ownedData.IsEquippable)
            return ActiveSkillItemVisualState.Upgrade;

        return currentCount >= RequiredMergeCount
            ? ActiveSkillItemVisualState.Enough
            : ActiveSkillItemVisualState.NotEnough;
    }

    // 현재 상태에서 잠금 해제/승급 버튼을 눌러도 되는지 판단합니다.
    private static bool CanTriggerStateAction(
        OwnedSkillData ownedData,
        ActiveSkillItemVisualState visualState,
        int currentCount)
    {
        if (visualState == ActiveSkillItemVisualState.Enough)
            return currentCount >= RequiredMergeCount;

        return false;
    }

    // 슬롯 타입과 스킬 타입이 서로 맞는지 검사합니다.
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

    private static SkillPresetSlot GetCurrentPresetSlot(int skillId)
    {
        SkillInventoryModule skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        SkillPreset currentPreset = skillModule?.GetCurrentPresetSnapshot();
        if (currentPreset?.slots == null)
            return null;

        for (int i = 0; i < currentPreset.slots.Length; i++)
        {
            SkillPresetSlot slot = currentPreset.slots[i];
            if (slot != null && slot.skillID == skillId)
                return slot;
        }

        return null;
    }

    private static int GetEquippedGemId(SkillPresetSlot presetSlot, int gemSlotIndex)
    {
        if (presetSlot == null)
            return SkillPresetSlot.EmptySkillId;

        switch (gemSlotIndex)
        {
            case 0:
            case 1:
                return presetSlot.m5JemIDs != null && gemSlotIndex < presetSlot.m5JemIDs.Length
                    ? presetSlot.m5JemIDs[gemSlotIndex]
                    : SkillPresetSlot.EmptySkillId;
            case 2:
                return presetSlot.m4JemID;
            default:
                return SkillPresetSlot.EmptySkillId;
        }
    }

    private static bool CanEquipToSlot(int slotIndex, SkillType type)
    {
        if (slotIndex == 2)
            return type == SkillType.ultimateSkil;

        return type == SkillType.basicSkill;
    }

    // 프리셋에 장착 가능한 액티브 스킬 타입인지 확인합니다.
    private static bool IsEquippableSkillType(SkillType skillType)
    {
        return skillType == SkillType.basicSkill || skillType == SkillType.ultimateSkil;
    }

    private static bool ShouldDisplayAsEmpty(SkillPresetSlot slot)
    {
        if (slot == null || slot.IsEmpty)
            return true;

        return DataManager.Instance == null
            || DataManager.Instance.SkillInfoDict == null
            || !DataManager.Instance.SkillInfoDict.ContainsKey(slot.skillID);
    }
}
