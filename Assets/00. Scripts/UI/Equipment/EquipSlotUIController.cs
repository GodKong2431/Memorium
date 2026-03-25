using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipSlotUIController : UIControllerBase
{
    private static readonly EquipmentType[] Order =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Armor,
        EquipmentType.Glove,
        EquipmentType.Boots
    };

    [SerializeField] private PlayerEquipment player;
    [SerializeField] private List<GameObject> slots = new List<GameObject>();

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
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
        for (int i = 0; i < Order.Length; i++)
        {
            EquipmentType type = Order[i];
            RefreshOne(type, player.ReturnItemNum(type));
        }
    }

    private void RefreshOne(EquipmentType type, int itemId)
    {
        int index = GetSlotIndex(type);
        if (index < 0 || index >= slots.Count)
            return;

        GameObject slot = slots[index];
        if (slot == null)
            return;

        if (itemId == 0 || !DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
        {
            ClearSlot(slot);
            return;
        }

        RenderSlot(slot, info);
    }

    private bool IsReady()
    {
        if (!TryResolvePlayer())
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        return HasAllSlots();
    }

    private bool TryResolvePlayer()
    {
        if (player != null)
            return true;

        EquipmentHandler equipmentHandler = EquipmentHandler.Instance;
        if (equipmentHandler != null && equipmentHandler.TryGetPlayerEquipment(out PlayerEquipment currentPlayer))
        {
            player = currentPlayer;
            return true;
        }

        return false;
    }

    private bool HasAllSlots()
    {
        if (slots.Count != Order.Length)
            return false;

        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i] == null)
                return false;
        }

        return true;
    }

    private static int GetSlotIndex(EquipmentType type)
    {
        for (int i = 0; i < Order.Length; i++)
        {
            if (Order[i] == type)
                return i;
        }

        return -1;
    }

    private static void RenderSlot(GameObject slot, EquipListTable info)
    {
        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            if (slot.GetComponent<Toggle>() != null)
                image.color = Color.white;
            else
                image.color = RarityColor.ItemGradeColor(info.rarityType);
        }

        string label = info.description + "\n" + info.equipmentName;

        Text legacyText = slot.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = label;

        TMP_Text tmpText = slot.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = label;
    }

    private static void ClearSlot(GameObject slot)
    {
        Image image = slot.GetComponent<Image>();
        if (image != null)
            image.color = Color.white;

        Text legacyText = slot.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = string.Empty;

        TMP_Text tmpText = slot.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = string.Empty;
    }
}
