using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleSkillPresenter : MonoBehaviour
{
    [SerializeField] private BattleSkillSlotView[] slotViews;

    private ISkillCooldownProvider cooldownProvider;


    private void Awake()
    {
        if (slotViews == null || slotViews.Length == 0)
            slotViews = GetComponentsInChildren<BattleSkillSlotView>();
    }

    public void Init(ISkillCooldownProvider _cooldownprovider)
    {
        cooldownProvider = _cooldownprovider;
        RebuildIcons();
    }

    private void OnEnable()
    {
        SkillInventoryManager.OnPresetChanged += OnPresetChanged;
    }

    private void OnDisable()
    {
        SkillInventoryManager.OnPresetChanged -= OnPresetChanged;
    }

    private void Update()
    {
        if (cooldownProvider == null) return;

        for(int i = 0; i < slotViews.Length; i++)
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
        var preset= SkillInventoryManager.Instance.GetCurrentPreset();
        if (preset == null) return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            var slot = preset.slots[i];
            if(slot.IsEmpty)
            {
                slotViews[i].SetEmpty();
                continue;
            }
            // 아이콘 확정시 스킬 id에 따른 아이콘 변경함수 추가
        }
    }
}