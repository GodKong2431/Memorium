using System;
using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public static event Action<EquipmentType, int> EquippedItemChanged;

    public EquipWeaponTable weapon;
    public EquipHelmetTable helmet;
    public EquipGloveTable glove;
    public EquipArmorTable armor;
    public EquipBootsTable boots;

    public EquipmentHandler equipmentHandler;

    public void OnEqipItem(int itemId)
    {
        if (!DataManager.Instance.EquipListDict.ContainsKey(itemId))
            return;

        // 장착 데이터와 PlayerSlot을 동시에 갱신한다.
        EquipListTable equipment = DataManager.Instance.EquipListDict[itemId];

        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                weapon = DataManager.Instance.EquipWeaponDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Weapon);
                break;
            case EquipmentType.Helmet:
                helmet = DataManager.Instance.EquipHelmetDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Helmet);
                break;
            case EquipmentType.Glove:
                glove = DataManager.Instance.EquipGloveDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Glove);
                break;
            case EquipmentType.Armor:
                armor = DataManager.Instance.EquipArmorDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Armor);
                break;
            case EquipmentType.Boots:
                boots = DataManager.Instance.EquipBootsDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Boots);
                break;
        }

        CharacterStatManager.Instance.SaveOnEquip(itemId, equipment.equipmentType);
        RaiseEquippedItemChanged(equipment.equipmentType, itemId);
    }

    public int ReturnItemNum(EquipmentType equipmentType)
    {
        // 타입에 해당하는 현재 장착 장비 ID를 반환한다.
        int itemId = 0;
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                itemId = weapon.ID;
                break;
            case EquipmentType.Helmet:
                itemId = helmet.ID;
                break;
            case EquipmentType.Glove:
                itemId = glove.ID;
                break;
            case EquipmentType.Armor:
                itemId = armor.ID;
                break;
            case EquipmentType.Boots:
                itemId = boots.ID;
                break;
        }

        return itemId;
    }

    private static void RaiseEquippedItemChanged(EquipmentType equipmentType, int itemId)
    {
        EquippedItemChanged?.Invoke(equipmentType, itemId);
    }
}
