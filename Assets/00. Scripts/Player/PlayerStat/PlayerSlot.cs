using System;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]

public class PlayerSlot
{
    public int CharacterStatID;
    public int SkillSlot1;
    public int SkillSlot2;
    public int SkillSlot3;
    public int WeaponSlot;
    public int HelmetSlot;
    public int ArmorSlot;
    public int GloveSlot;
    public int BootsSlot;
    public int FairySlot;

    public event Action OnSlotUpdate;

    public PlayerSlot(int key)
    {
        DataManager.Instance.CharacterDict.TryGetValue(key, out var value);

        CharacterStatID = value.characterStatID;
        SkillSlot1 = value.skillSlot1;
        SkillSlot2 = value.skillSlot2;
        SkillSlot3 = value.skillSlot3;
        WeaponSlot = value.weaponSlot;
        HelmetSlot = value.helmetSlot;
        ArmorSlot = value.armorSlot;
        GloveSlot = value.gloveSlot;
        BootsSlot = value.bootsSlot;
        FairySlot = value.fairySlot;
    }

    public void SetSlot(int key, SlotType slotType)
    {
        switch (slotType)
        {
            case SlotType.Skill1:
                SkillSlot1 = key;
                break;
            case SlotType.Skill2:
                SkillSlot2 = key;
                break;
            case SlotType.Skill3:
                SkillSlot3 = key;
                break;
            case SlotType.Weapon:
                WeaponSlot = key;
                break;
            case SlotType.Helmet:
                HelmetSlot = key;
                break;
            case SlotType.Armor:
                ArmorSlot = key;
                break;
            case SlotType.Glove:
                GloveSlot = key;
                break;
            case SlotType.Boots:
                BootsSlot = key;
                break;
            case SlotType.Fairy:
                FairySlot = key;
                break;
        }
        OnSlotUpdate?.Invoke();
    }

    public float GetStat(PlayerStatType playerStatType)
    {
        switch (playerStatType)
        {
            case PlayerStatType.ATK:
                DataManager.Instance.EquipWeaponDict.TryGetValue(WeaponSlot, out var valueWeaponStat1);
                return valueWeaponStat1?.attackPower ?? 0f;
            case PlayerStatType.ATK_SPEED:
                DataManager.Instance.EquipWeaponDict.TryGetValue(WeaponSlot, out var valueWeaponStat2);
                return valueWeaponStat2?.attackSpeed ?? 0f;
            case PlayerStatType.HP:
                DataManager.Instance.EquipArmorDict.TryGetValue(ArmorSlot, out var valueArmor);
                return valueArmor?.hp ?? 0f;
            case PlayerStatType.MAGIC_DEF:
                DataManager.Instance.EquipGloveDict.TryGetValue(GloveSlot, out var valueGlove);
                return valueGlove?.magicDefense ?? 0f;
            case PlayerStatType.MOVE_SPEED:
                DataManager.Instance.EquipBootsDict.TryGetValue(BootsSlot, out var valueBoots);
                return valueBoots?.moveSpeed ?? 0f;
            default:
                return 0f;
        }
    }
}
