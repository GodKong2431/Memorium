using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 중 노출되는 장착 스킬 슬롯과 쿨타임 UI를 갱신합니다.
/// </summary>
public class BattleSkillPresenter : MonoBehaviour
{
    // 현재 프리셋을 표시할 슬롯 뷰 배열입니다.
    [SerializeField] private BattleSkillSlotView[] slotViews;

    // 현재 바인딩된 슬롯 개수입니다.
    public int SlotCount => slotViews != null ? slotViews.Length : 0;

    // 쿨타임 정보를 제공하는 외부 주체입니다.
    private ISkillCooldownProvider cooldownProvider;
    // 프리셋 변경 이벤트를 듣기 위한 인벤토리 모듈 참조입니다.
    private SkillInventoryModule subscribedSkillModule;
    private readonly Dictionary<int, Sprite> gemIconCache = new Dictionary<int, Sprite>();
    // 플레이어 쿨타임 공급자를 연결하고 즉시 화면을 갱신합니다.
    public void BindCooldownProvider(ISkillCooldownProvider cooldownProvider)
    {
        this.cooldownProvider = cooldownProvider;
        RefreshSlots();
        RefreshCooldowns();
    }

    // 활성화될 때 프리셋 구독을 연결하고 슬롯을 갱신합니다.
    private void OnEnable()
    {
        GameEventManager.OnPlayerSpawned += OnPlayerSpawned;
        EnsureSkillModuleSubscription();
        TryBindCurrentPlayer();
        RefreshSlots();
    }

    // 비활성화될 때 프리셋 구독을 해제합니다.
    private void OnDisable()
    {
        GameEventManager.OnPlayerSpawned -= OnPlayerSpawned;
        UnsubscribeSkillModule();
    }

    // 매 프레임 슬롯별 쿨타임 fill 값을 갱신합니다.
    private void Update()
    {
        EnsureSkillModuleSubscription();
        RefreshCooldowns();
    }

    // 프리셋이 바뀌면 아이콘 구성을 다시 그립니다.
    private void OnPresetChanged(int presetIndex)
    {
        RefreshSlots();
    }

    private void OnPlayerSpawned(Transform playerTransform)
    {
        BindPlayer(playerTransform);
    }

    private void ResetForSceneChange()
    {
        BindCooldownProvider(null);
    }

    private void TryBindCurrentPlayer()
    {
        if (ScenePlayerLocator.TryGetPlayerTransform(out Transform playerTransform))
            BindPlayer(playerTransform);
        else
            BindCooldownProvider(null);
    }

    private void BindPlayer(Transform playerTransform)
    {
        PlayerSkillHandler playerSkillHandler = playerTransform != null
            ? playerTransform.GetComponent<PlayerSkillHandler>()
            : null;

        BindCooldownProvider(playerSkillHandler);
    }

    // 스킬 인벤토리 모듈 변경을 감지하고 이벤트를 연결합니다.
    private void EnsureSkillModuleSubscription()
    {
        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null || subscribedSkillModule == skillModule)
            return;

        UnsubscribeSkillModule();
        subscribedSkillModule = skillModule;
        subscribedSkillModule.OnPresetChanged += OnPresetChanged;
    }

    // 연결된 인벤토리 모듈 이벤트를 정리합니다.
    private void UnsubscribeSkillModule()
    {
        if (subscribedSkillModule == null)
            return;

        subscribedSkillModule.OnPresetChanged -= OnPresetChanged;
        subscribedSkillModule = null;
    }

    // 현재 프리셋 기준으로 슬롯 아이콘을 새로 그립니다.
    public void RefreshSlots()
    {
        if (slotViews == null || slotViews.Length == 0)
            return;

        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        if (skillModule == null)
        {
            ClearAllSlots();
            return;
        }

        var preset = skillModule.GetCurrentPresetSnapshot();
        if (preset == null)
        {
            ClearAllSlots();
            return;
        }

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
                continue;

            if (i >= preset.slots.Length)
            {
                slotViews[i].SetEmpty();
                continue;
            }

            var slot = preset.slots[i];
            if (ShouldDisplayAsEmpty(slot))
            {
                slotViews[i].SetEmpty();
                continue;
            }
            SkillPresetSlot presetSlot = GetCurrentPresetSlot(slot.skillID);
            Sprite[] gemIcons = BuildEquippedGemIcons(presetSlot);

            int level = GetSkillLevel(slot.skillID);
            int gemCount = GetOpenGemCount(slot.skillID);

            slotViews[i].SetSkillDisplay(slot.skillID, GetSkillIcon(slot.skillID), level, gemCount, gemIcons);
        }
    }
    private Sprite[] BuildEquippedGemIcons(SkillPresetSlot slot)
    {
        Sprite[] icons = new Sprite[3];
        if (slot == null || slot.IsEmpty) return icons;

        for (int i = 0; i < 3; i++)
        {
            int gemId = GetEquippedGemId(slot, i);
            icons[i] = gemId > 0 ? GetGemIcon(gemId) : null;
        }
        return icons;
    }

    private Sprite GetGemIcon(int itemId)
    {
        if (itemId <= 0) return null;

        if (gemIconCache.TryGetValue(itemId, out Sprite cached))
            return cached;

        var gemModule = InventoryManager.Instance?.GetModule<GemInventoryModule>();
        if (gemModule == null) return null;

        Sprite sprite = null;

        int m4Id = gemModule.GetM4IdByItemId(itemId);
        if (m4Id != 0 && DataManager.Instance.SkillModule4Dict.TryGetValue(m4Id, out var m4Data))
        {
            sprite = SkillIconResolver.TryLoad(m4Data.m4Icon);
        }

        if (sprite == null)
        {
            int m5Id = gemModule.GetM5IdByItemId(itemId);
            if (m5Id != 0 && DataManager.Instance.SkillModule5Dict.TryGetValue(m5Id, out var m5Data))
            {
                sprite = SkillIconResolver.TryLoad(m5Data.m5Icon);
            }
        }

        if (sprite != null)
        {
            gemIconCache[itemId] = sprite;
        }

        return sprite;
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
    private int[] GetEquippedGemItemIds(int skillId)
    {
        var skillModule = InventoryManager.Instance?.GetModule<SkillInventoryModule>();
        SkillGemSlotData gemData = skillModule?.GetGemSlotData(skillId);

        int[] gemItemIds = new int[3];
        if (gemData != null)
        {
            gemItemIds[0] = gemData.m5JemIDs != null && gemData.m5JemIDs.Length > 0 && gemData.m5JemIDs[0] > 0 ? gemData.m5JemIDs[0] : 0;
            gemItemIds[1] = gemData.m5JemIDs != null && gemData.m5JemIDs.Length > 1 && gemData.m5JemIDs[1] > 0 ? gemData.m5JemIDs[1] : 0;
            gemItemIds[2] = gemData.m4JemID > 0 ? gemData.m4JemID : 0;
        }

        return gemItemIds;
    }
    private void RefreshCooldowns()
    {
        if (slotViews == null)
            return;

        if (cooldownProvider == null)
        {
            ClearCooldowns();
            return;
        }

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
                continue;

            float remain = cooldownProvider.GetCooldownRemain(i);
            float max = cooldownProvider.GetCooldownMax(i);

            if (remain > 0f && max > 0f)
                slotViews[i].UpdateCooldown(remain / max, remain);
            else
                slotViews[i].UpdateCooldown(0f, 0f);
        }
    }

    // 특정 슬롯 버튼 클릭 시 호출할 콜백을 연결합니다.
    public void SetSlotClickListener(int index, Action callback)
    {
        if (slotViews == null || index < 0 || index >= slotViews.Length)
            return;

        slotViews[index].SetClickListener(callback);
    }

    // 특정 슬롯의 강조 상태를 색상으로 표현합니다.
    public void SetSlotHighlight(int index, bool selected)
    {
        if (slotViews == null || index < 0 || index >= slotViews.Length)
            return;

        if (selected)
            slotViews[index].SetButtonColor(Color.gold);
        else
            slotViews[index].ResetButtonColor();
    }

    // 모든 슬롯을 빈 상태로 초기화합니다.
    private void ClearAllSlots()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
                continue;

            slotViews[i].SetEmpty();
        }
    }

    private void ClearCooldowns()
    {
        if (slotViews == null)
            return;

        for (int i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] == null)
                continue;

            slotViews[i].UpdateCooldown(0f, 0f);
        }
    }

    // 스킬 ID에 대응하는 아이콘을 데이터 테이블에서 가져옵니다.
    private static Sprite GetSkillIcon(int skillId)
    {
        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return null;

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out SkillInfoTable table))
            return null;

        return SkillIconResolver.TryLoad(table.skillIcon, skillId);
    }

    private static bool ShouldDisplayAsEmpty(SkillPresetSlot slot)
    {
        if (slot == null || slot.IsEmpty)
            return true;

        return DataManager.Instance == null
            || DataManager.Instance.SkillInfoDict == null
            || !DataManager.Instance.SkillInfoDict.ContainsKey(slot.skillID);
    }
    private static int GetSkillLevel(int skillId)
    {
        var skillModule = InventoryManager.Instance?.GetModule<SkillInventoryModule>();
        var data = skillModule?.GetSkillData(skillId);
        return data != null ? data.level : 0;
    }

    private static int GetOpenGemCount(int skillId)
    {
        var skillModule = InventoryManager.Instance?.GetModule<SkillInventoryModule>();
        var data = skillModule?.GetSkillData(skillId);
        if (data == null) return 0;

        int count = 0;
        if (data.IsM5JemSlotOpen(0)) count++;
        if (data.IsM5JemSlotOpen(1)) count++;
        if (data.IsM4JemSlotOpen) count++;
        return count;
    }
}
