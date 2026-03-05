using System;
using UnityEngine;

public class BattleSkillPresenter : MonoBehaviour
{
    [SerializeField] private BattleSkillSlotView[] slotViews;

    public int SlotCount => slotViews.Length;

    private ISkillCooldownProvider cooldownProvider;

    private void Awake()
    {
        if (slotViews == null || slotViews.Length == 0)
            slotViews = GetComponentsInChildren<BattleSkillSlotView>();
    }

    public void Init(ISkillCooldownProvider cooldownProvider)
    {
        this.cooldownProvider = cooldownProvider;
        RebuildIcons();
    }

    private void OnEnable()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule != null)
            skillModule.OnPresetChanged += OnPresetChanged;
    }

    private void OnDisable()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule != null)
            skillModule.OnPresetChanged -= OnPresetChanged;
    }

    private void Update()
    {
        if (cooldownProvider == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            float remain = cooldownProvider.GetCooldownRemain(i);
            float max = cooldownProvider.GetCooldownMax(i);

            if (remain > 0f && max > 0f)
                slotViews[i].UpdateCooldown(remain / max, remain);
            else
                slotViews[i].UpdateCooldown(0f, 0f);
        }
    }

    private void OnPresetChanged(int presetIndex)
    {
        RebuildIcons();
    }

    private void RebuildIcons()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null)
            return;

        var preset = skillModule.GetCurrentPreset();
        if (preset == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            var slot = preset.slots[i];
            if (slot.IsEmpty)
            {
                slotViews[i].SetEmpty();
                continue;
            }

            // 아이콘 확정 시 스킬 ID에 맞는 아이콘 반영 로직을 추가.
        }
    }

    public void SetSlotClickListener(int index, Action callback)
    {
        slotViews[index].SlotButton.onClick.AddListener(() => callback());
    }

    public void SetSlotHighlight(int index, bool selected)
    {
        var btn = slotViews[index].SlotButton;
        var colors = btn.colors;
        Color color = selected ? Color.gold : Color.white;
        colors.normalColor = color;
        colors.highlightedColor = color;
        colors.selectedColor = color;
        btn.colors = colors;
    }
}
