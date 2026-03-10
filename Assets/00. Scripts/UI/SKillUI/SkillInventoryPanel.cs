using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SkillInventoryPanel : MonoBehaviour
{
    [Header("프리셋 버튼")]
    [SerializeField] private Button[] presetButtons;

    [Header("프리셋 스킬 창")]
    [SerializeField] private BattleSkillPresenter battleSkillPresenter;

    [Header("스크롤뷰")]
    [SerializeField] private Transform scrollContent;
    [SerializeField] private SkillSlotItem slotPrefab;

    [Header("전체 합성")]
    [SerializeField] private Button mergeAllButton;

    [Header("빈 곳 클릭 캔슬용 패널")]
    [SerializeField] private Button backgroundButton;

    private int selectedSkillID = -1;
    private SkillType selectedSkillType;

    private readonly List<SkillSlotItem> spawnedSlots = new List<SkillSlotItem>();
    private bool isDirty;

    private void Start()
    {
        if (battleSkillPresenter == null)
            battleSkillPresenter = FindAnyObjectByType<BattleSkillPresenter>();

        InitButtons();
    }

    private void OnEnable()
    {
        var skillModule = GetSkillModule();
        if (skillModule != null)
        {
            skillModule.OnInventoryChanged += MarkDirty;
            skillModule.OnPresetChanged += OnPresetChanged;
        }

        isDirty = true;
    }

    private void OnDisable()
    {
        var skillModule = GetSkillModule();
        if (skillModule != null)
        {
            skillModule.OnInventoryChanged -= MarkDirty;
            skillModule.OnPresetChanged -= OnPresetChanged;
        }
    }

    private void LateUpdate()
    {
        if (!isDirty)
            return;

        isDirty = false;
        RebuildAll();
    }

    private void MarkDirty()
    {
        isDirty = true;
    }

    private void InitButtons()
    {
        for (int i = 0; i < presetButtons.Length; i++)
        {
            int idx = i;
            presetButtons[i].onClick.AddListener(() => OnPresetTabClicked(idx));
        }

        for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
        {
            int idx = i;
            battleSkillPresenter.SetSlotClickListener(idx, () => OnPresetSlotClicked(idx));
        }

        mergeAllButton.onClick.AddListener(OnMergeAllClicked);
        if (backgroundButton != null)
            backgroundButton.onClick.AddListener(CancelSelection);
    }

    private void OnPresetTabClicked(int index)
    {
        GetSkillModule()?.SwitchPreset(index);
    }

    private void OnPresetChanged(int presetIndex)
    {
        CancelSelection();
        isDirty = true;
    }

    private void OnPresetSlotClicked(int slotIndex)
    {

        if (selectedSkillID < 0) return;

        if (!CanEquipToSlot(slotIndex, selectedSkillType))
        {

            return;
        }

        // 장착
        var skillModule = GetSkillModule();
        if (skillModule != null)
        {
            skillModule.SetPresetSlot(slotIndex, selectedSkillID);
        }

        CancelSelection();
    }

    private bool CanEquipToSlot(int slotIndex, SkillType type)
    {
        if (slotIndex == 2)
            return type == SkillType.ultimateSkil;
        else
            return type == SkillType.basicSkill;
    }
    private void CancelSelection()
    {
        selectedSkillID = -1;
        ClearSlotHighlights();
    }
    private void UpdateSlotHighlights()
    {
        for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
       {
            bool canEquip = CanEquipToSlot(i, selectedSkillType);
            battleSkillPresenter.SetSlotHighlight(i, canEquip);
        }
    }
    private void ClearSlotHighlights()
    {
        for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
            battleSkillPresenter.SetSlotHighlight(i, false);
    }

    private void RebuildAll()
    {
        RebuildPresetTabs();
        RebuildSkillList();
    }
    private void RebuildPresetTabs()
    {
        var skillModule = GetSkillModule();
        if (skillModule == null) return;

        int current = skillModule.CurrentPresetIndex;
        for (int i = 0; i < presetButtons.Length; i++)
        {
            var colors = presetButtons[i].colors;
            Color color = i == current ? Color.skyBlue : Color.white;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.selectedColor = color;
            presetButtons[i].colors = colors;
        }
    }

    private void RebuildSkillList()
    {
        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return;

        var allSkills = DataManager.Instance.SkillInfoDict;

        while (spawnedSlots.Count < allSkills.Count)
        {
            var slotUI = Instantiate(slotPrefab, scrollContent);
            spawnedSlots.Add(slotUI);
        }

        int index = 0;
        foreach (var kvp in allSkills)
        {
            spawnedSlots[index].Init(kvp.Key, OnEquipSkillClicked);
            spawnedSlots[index].gameObject.SetActive(true);
            index++;
        }

        for (int i = index; i < spawnedSlots.Count; i++)
            spawnedSlots[i].gameObject.SetActive(false);
    }

    private void OnEquipSkillClicked(int skillId)
    {
        var skillModule = GetSkillModule();
        if (skillModule == null) return;

        var owned = skillModule.GetSkillData(skillId);
        if (owned == null) return;

        var preset = skillModule.GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (preset.slots[i].skillID == skillId)
            {

                return;
            }
        }

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out var table))
            return;

        selectedSkillID = skillId;
        selectedSkillType = table.skillType;

        UpdateSlotHighlights();
    }

    private void OnMergeAllClicked()
    {
        GetSkillModule()?.MergeAllSkills();
    }
    private SkillInventoryModule GetSkillModule()
    {
        return InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
    }
}