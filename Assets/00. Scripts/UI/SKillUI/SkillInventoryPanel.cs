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

    private int selectedPresetSlot = -1;
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
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule != null)
        {
            skillModule.OnInventoryChanged += MarkDirty;
            skillModule.OnPresetChanged += OnPresetChanged;
        }

        isDirty = true;
    }

    private void OnDisable()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
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
    }

    private void OnPresetTabClicked(int index)
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        skillModule?.SwitchPreset(index);
    }

    private void OnPresetChanged(int presetIndex)
    {
        selectedPresetSlot = -1;
        isDirty = true;
    }

    private void OnPresetSlotClicked(int slotIndex)
    {
        selectedPresetSlot = selectedPresetSlot == slotIndex ? -1 : slotIndex;
        RebuildPresetSlotHighlight();
    }

    private void RebuildPresetSlotHighlight()
    {
        for (int i = 0; i < battleSkillPresenter.SlotCount; i++)
            battleSkillPresenter.SetSlotHighlight(i, i == selectedPresetSlot);
    }

    private void RebuildAll()
    {
        RebuildPresetTabs();
        RebuildSkillList();
    }

    private void RebuildPresetTabs()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null)
            return;

        int current = skillModule.CurrentPresetIndex;
        for (int i = 0; i < presetButtons.Length; i++)
        {
            var colors = presetButtons[i].colors;
            Color color = i == current ? Color.lightSkyBlue : Color.white;
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
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null)
            return;

        int targetSlot = selectedPresetSlot;

        if (targetSlot < 0)
        {
            targetSlot = FindEmptyPresetSlot();
            if (targetSlot < 0)
            {
                Debug.Log("빈 프리셋 슬롯이 없습니다. 슬롯을 먼저 선택하세요.");
                return;
            }
        }

        var owned = skillModule.GetSkillData(skillId);
        if (owned == null)
            return;

        var preset = skillModule.GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (i != targetSlot && preset.slots[i].skillID == skillId)
            {
                Debug.Log("이미 장착된 스킬입니다.");
                return;
            }
        }

        skillModule.SetPresetSlot(targetSlot, skillId);
        selectedPresetSlot = -1;
    }

    private int FindEmptyPresetSlot()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null)
            return -1;

        var preset = skillModule.GetCurrentPreset();
        for (int i = 0; i < preset.slots.Length; i++)
        {
            if (preset.slots[i].IsEmpty)
                return i;
        }

        return -1;
    }

    private void OnMergeAllClicked()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        skillModule?.MergeAllSkills();
    }
}
