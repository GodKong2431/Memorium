using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipSlotUIController : UIControllerBase
{
    [SerializeField] private PlayerEquipment player;

    [SerializeField] private List<GameObject> slots = new List<GameObject>();

    private readonly List<EquipSlotView> views = new List<EquipSlotView>();

    protected override void Initialize()
    {
        views.Clear();

        for (int i = 0; i < slots.Count; i++)
            views.Add(new EquipSlotView(slots[i]));
    }

    protected override void Subscribe()
    {
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;
    }

    protected override void Unsubscribe()
    {
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;
    }

    protected override void RefreshView()
    {
        if (!IsReady())
            return;

        RefreshAll();
    }

    private void HandleEquippedChanged(EquipmentType type, int itemId)
    {
        if (!IsReady())
            return;

        RefreshOne(type, itemId);
    }

    private void RefreshAll()
    {
        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int itemId = player.ReturnItemNum(type);
            RefreshOne(type, itemId);
        }
    }

    private void RefreshOne(EquipmentType type, int itemId)
    {
        int index = (int)type - (int)EquipmentType.Weapon;
        if (index < 0 || index >= views.Count)
            return;
        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
            return;

        // 장착 슬롯의 단순 표시(View)만 갱신한다.
        views[index].Render(info);
    }

    private bool IsReady()
    {
        if (player == null)
            return false;
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;
        if (views.Count == 0)
            return false;

        return true;
    }
}

