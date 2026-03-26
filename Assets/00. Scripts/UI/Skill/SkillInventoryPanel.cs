using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레거시 프리셋 스킬 패널의 탭, 슬롯, 목록, 합성 버튼을 관리합니다.
/// </summary>
public class SkillInventoryPanel : MonoBehaviour
{
    [Header("프리셋 버튼")]
    // 프리셋 전환 버튼 배열입니다.
    [SerializeField] private Button[] presetButtons;

    [Header("프리셋 스킬 창")]
    // 현재 프리셋 슬롯을 보여주는 프레젠터입니다.
    [SerializeField] private BattleSkillPresenter battleSkillPresenter;

    [Header("스크롤뷰")]
    // 스킬 아이템이 생성될 스크롤 콘텐츠입니다.
    [SerializeField] private Transform scrollContent;
    // 스킬 슬롯 카드 프리팹입니다.
    [SerializeField] private SkillSlotItem slotPrefab;

    [Header("전체 합성")]
    // 전체 합성 버튼입니다.
    [SerializeField] private Button mergeAllButton;

    [Header("빈 곳 클릭 캔슬용 패널")]
    // 빈 배경 클릭 시 선택을 취소하는 버튼입니다.
    [SerializeField] private Button backgroundButton;

    // 현재 장착 대기 중인 스킬 ID입니다.
    private int selectedSkillID = -1;
    // 현재 장착 대기 중인 스킬 타입입니다.
    private SkillType selectedSkillType;

    // 생성된 슬롯 카드 인스턴스 목록입니다.
    private readonly List<SkillSlotItem> spawnedSlots = new List<SkillSlotItem>();
    // 현재 구독 중인 스킬 인벤토리 모듈입니다.
    private SkillInventoryModule subscribedSkillModule;
    // 프리셋 버튼 이벤트 연결 여부입니다.
    private bool presetButtonsBound;
    // 슬롯 버튼 이벤트 연결 여부입니다.
    private bool slotButtonsBound;
    // 기타 버튼 이벤트 연결 여부입니다.
    private bool utilityButtonsBound;
    // 다음 프레임에 전체 갱신이 필요한지 표시합니다.
    private bool isDirty;

    // 최초 진입 시 UI 이벤트 연결을 시도합니다.
    private void Awake()
    {
        TryInitializeUi();
    }

    // 활성화될 때 모듈 구독을 연결하고 갱신 플래그를 세웁니다.
    private void OnEnable()
    {
        TryInitializeUi();
        EnsureSkillModuleSubscription();
        MarkDirty();
    }

    // 비활성화될 때 모듈 구독을 해제합니다.
    private void OnDisable()
    {
        UnsubscribeSkillModule();
    }

    // 준비가 끝난 뒤 실제 화면 재구성을 처리합니다.
    private void LateUpdate()
    {
        EnsureSkillModuleSubscription();
        TryInitializeUi();

        if (!isDirty)
            return;

        if (!IsReadyToRender())
            return;

        isDirty = false;
        RebuildAll();
    }

    // 다음 프레임에 다시 그리도록 표시합니다.
    private void MarkDirty()
    {
        isDirty = true;
    }

    // 인벤토리 모듈 변경을 감지하고 이벤트를 구독합니다.
    private void EnsureSkillModuleSubscription()
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null || subscribedSkillModule == skillModule)
            return;

        UnsubscribeSkillModule();
        subscribedSkillModule = skillModule;
        subscribedSkillModule.OnInventoryChanged += MarkDirty;
        subscribedSkillModule.OnPresetChanged += OnPresetChanged;
        MarkDirty();
    }

    // 이전 인벤토리 모듈 이벤트를 정리합니다.
    private void UnsubscribeSkillModule()
    {
        if (subscribedSkillModule == null)
            return;

        subscribedSkillModule.OnInventoryChanged -= MarkDirty;
        subscribedSkillModule.OnPresetChanged -= OnPresetChanged;
        subscribedSkillModule = null;
    }

    // 아직 연결되지 않은 버튼과 슬롯 이벤트를 한 번만 연결합니다.
    private void TryInitializeUi()
    {
        if (!presetButtonsBound && presetButtons != null)
        {
            for (int i = 0; i < presetButtons.Length; i++)
            {
                if (presetButtons[i] == null)
                    continue;

                int idx = i;
                presetButtons[i].onClick.AddListener(() => OnPresetTabClicked(idx));
                UiButtonSoundPlayer.Ensure(presetButtons[i], UiSoundIds.DefaultButton);
            }

            presetButtonsBound = true;
        }

        if (!slotButtonsBound && battleSkillPresenter != null)
        {
            for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
            {
                int idx = i;
                battleSkillPresenter.SetSlotClickListener(idx, () => OnPresetSlotClicked(idx));
            }

            slotButtonsBound = true;
        }

        if (utilityButtonsBound)
            return;

        if (mergeAllButton != null)
        {
            mergeAllButton.onClick.AddListener(OnMergeAllClicked);
            UiButtonSoundPlayer.Ensure(mergeAllButton, UiSoundIds.DefaultButton);
        }
        if (backgroundButton != null)
        {
            backgroundButton.onClick.AddListener(CancelSelection);
            UiButtonSoundPlayer.Ensure(backgroundButton, UiSoundIds.DefaultButton);
        }

        utilityButtonsBound = true;
    }

    // 프리셋 탭 버튼 클릭 시 프리셋을 전환합니다.
    private void OnPresetTabClicked(int index)
    {
        GetSkillModule()?.SwitchPreset(index);
    }

    // 프리셋이 바뀌면 선택 상태를 지우고 다시 그립니다.
    private void OnPresetChanged(int presetIndex)
    {
        CancelSelection();
        isDirty = true;
    }

    // 장착 슬롯 클릭 시 선택 중인 스킬을 해당 슬롯에 넣습니다.
    private void OnPresetSlotClicked(int slotIndex)
    {
        if (selectedSkillID < 0)
            return;

        if (!CanEquipToSlot(slotIndex, selectedSkillType))
            return;

        var skillModule = GetSkillModule();
        if (skillModule != null)
            skillModule.SetPresetSlot(slotIndex, selectedSkillID);

        CancelSelection();
    }

    // 슬롯 인덱스와 스킬 타입이 맞는지 검사합니다.
    private bool CanEquipToSlot(int slotIndex, SkillType type)
    {
        if (slotIndex == 2)
            return type == SkillType.ultimateSkil;

        return type == SkillType.basicSkill;
    }

    // 현재 장착 대기 상태와 하이라이트를 초기화합니다.
    private void CancelSelection()
    {
        selectedSkillID = -1;
        ClearSlotHighlights();
    }

    // 현재 선택된 스킬이 장착 가능한 슬롯만 강조합니다.
    private void UpdateSlotHighlights()
    {
        if (battleSkillPresenter == null)
            return;

        for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
        {
            bool canEquip = CanEquipToSlot(i, selectedSkillType);
            battleSkillPresenter.SetSlotHighlight(i, canEquip);
        }
    }

    // 모든 슬롯 강조 표시를 해제합니다.
    private void ClearSlotHighlights()
    {
        if (battleSkillPresenter == null)
            return;

        for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
            battleSkillPresenter.SetSlotHighlight(i, false);
    }

    // 프리셋 탭과 스킬 목록을 함께 갱신합니다.
    private void RebuildAll()
    {
        RebuildPresetTabs();
        RebuildSkillList();
    }

    // 현재 프리셋 인덱스에 따라 탭 색상을 갱신합니다.
    private void RebuildPresetTabs()
    {
        var skillModule = GetSkillModule();
        if (skillModule == null || presetButtons == null)
            return;

        int current = skillModule.CurrentPresetIndex;
        for (int i = 0; i < presetButtons.Length; i++)
        {
            if (presetButtons[i] == null)
                continue;

            var colors = presetButtons[i].colors;
            Color color = i == current ? Color.skyBlue : Color.white;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.selectedColor = color;
            presetButtons[i].colors = colors;
        }
    }

    // 표시 대상 스킬 목록을 재생성하거나 재사용 슬롯에 반영합니다.
    private void RebuildSkillList()
    {
        if (scrollContent == null || slotPrefab == null)
            return;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.SkillInfoDict == null)
            return;

        List<int> displaySkillIds = new List<int>();
        foreach (var pair in DataManager.Instance.SkillInfoDict)
        {
            if (!IsPresetEquippableSkill(pair.Value.skillType))
                continue;

            displaySkillIds.Add(pair.Key);
        }

        displaySkillIds.Sort((a, b) => a.CompareTo(b));

        while (spawnedSlots.Count < displaySkillIds.Count)
        {
            var slotUI = Instantiate(slotPrefab, scrollContent);
            spawnedSlots.Add(slotUI);
        }

        int index = 0;
        for (int i = 0; i < displaySkillIds.Count; i++)
        {
            int skillId = displaySkillIds[i];
            spawnedSlots[index].Init(skillId, OnEquipSkillClicked);
            spawnedSlots[index].gameObject.SetActive(true);
            index++;
        }

        for (int i = index; i < spawnedSlots.Count; i++)
            spawnedSlots[i].gameObject.SetActive(false);
    }

    // 리스트의 장착 버튼을 눌렀을 때 슬롯 선택 상태로 들어갑니다.
    private void OnEquipSkillClicked(int skillId)
    {
        var skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        var owned = skillModule.GetSkillData(skillId);
        if (owned == null)
            return;

        var preset = skillModule.GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (preset.slots[i].skillID == skillId)
                return;
        }

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out var table))
            return;

        selectedSkillID = skillId;
        selectedSkillType = table.skillType;

        UpdateSlotHighlights();
    }

    // 전체 합성 버튼 클릭 시 모든 스킬을 합성합니다.
    private void OnMergeAllClicked()
    {
        return;
    }

    // 프리셋에 장착 가능한 스킬 타입만 통과시킵니다.
    private static bool IsPresetEquippableSkill(SkillType skillType)
    {
        return skillType == SkillType.basicSkill || skillType == SkillType.ultimateSkil;
    }

    // 현재 인벤토리 모듈을 가져옵니다.
    private SkillInventoryModule GetSkillModule()
    {
        return InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
    }

    // 목록 재구성에 필요한 참조와 데이터가 준비됐는지 검사합니다.
    private bool IsReadyToRender()
    {
        return GetSkillModule() != null
            && scrollContent != null
            && slotPrefab != null
            && DataManager.Instance != null
            && DataManager.Instance.DataLoad
            && DataManager.Instance.SkillInfoDict != null;
    }
}
