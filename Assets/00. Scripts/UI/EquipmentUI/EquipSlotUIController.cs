using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipSlotUIController : UIControllerBase
{
    [SerializeField] private PlayerEquipment player;

    [SerializeField] private List<GameObject> slots = new List<GameObject>();

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
        if (index < 0 || index >= slots.Count)
            return;
        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
            return;

        GameObject slot = slots[index];
        if (slot == null)
            return;

        RenderSlot(slot, info);
    }

    private static void RenderSlot(GameObject slot, EquipListTable info)
    {
        // Toggle 슬롯은 원색 유지, 일반 슬롯은 희귀도 색으로 표시한다.
        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            if (slot.GetComponent<Toggle>() != null)
                image.color = Color.white;
            else
                image.color = RarityColor.ItemGradeColor(info.rarityType);
        }

        string label = info.description + "\n" + info.equipmentName;

        // 기존 Text/TMP_Text 중 씬에 있는 타입에 맞춰 텍스트를 갱신한다.
        Text legacyText = slot.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = label;

        TMP_Text tmpText = slot.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = label;
    }

    private bool IsReady()
    {
        if (player == null)
            return false;
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;
        if (slots.Count == 0)
            return false;

        return true;
    }
}
