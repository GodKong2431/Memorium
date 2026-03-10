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

    public float GetStat(StatType playerStatType)
    {
        switch (playerStatType)
        {
            case StatType.ATK:
                DataManager.Instance.EquipWeaponDict.TryGetValue(WeaponSlot, out var valueWeaponStat1);
                //Debug.Log($"[PlayerSlot] 강화 공격력 : {ReinforecementEquipmentStat.ReturnReinforceStat(WeaponSlot, playerStatType)}  보너스 공격력 : {ReinforecementEquipmentStat.ReturnBonusStat(playerStatType)}");
                return valueWeaponStat1?.attackPower + ReinforecementEquipmentStat.ReturnReinforceStat(WeaponSlot, playerStatType)
                    + ReinforecementEquipmentStat.ReturnBonusStat(playerStatType)?? 0f;
            case StatType.ATK_SPEED:
                DataManager.Instance.EquipWeaponDict.TryGetValue(WeaponSlot, out var valueWeaponStat2);
                //Debug.Log($"[PlayerSlot] 강화 공격속도: {ReinforecementEquipmentStat.ReturnReinforceStat(WeaponSlot, playerStatType)}  보너스 공격속도 : {ReinforecementEquipmentStat.ReturnBonusStat(playerStatType)}");
                return valueWeaponStat2?.attackSpeed + ReinforecementEquipmentStat.ReturnReinforceStat(WeaponSlot, playerStatType)
                    + ReinforecementEquipmentStat.ReturnBonusStat(playerStatType) ?? 0f;

            case StatType.PHYS_DEF:
                DataManager.Instance.EquipHelmetDict.TryGetValue(HelmetSlot, out var valueHelmet);
                return valueHelmet?.defense + ReinforecementEquipmentStat.ReturnReinforceStat(HelmetSlot, playerStatType)
                    + ReinforecementEquipmentStat.ReturnBonusStat(playerStatType) ?? 0f;
            case StatType.HP:
                DataManager.Instance.EquipArmorDict.TryGetValue(ArmorSlot, out var valueArmor);
                return valueArmor?.hp + ReinforecementEquipmentStat.ReturnReinforceStat(ArmorSlot, playerStatType)
                    + ReinforecementEquipmentStat.ReturnBonusStat(playerStatType) ?? 0f;
            case StatType.MAGIC_DEF:
                DataManager.Instance.EquipGloveDict.TryGetValue(GloveSlot, out var valueGlove);
                return valueGlove?.magicDefense + ReinforecementEquipmentStat.ReturnReinforceStat(GloveSlot, playerStatType)
                    + ReinforecementEquipmentStat.ReturnBonusStat(playerStatType) ?? 0f;
            case StatType.MOVE_SPEED:
                DataManager.Instance.EquipBootsDict.TryGetValue(BootsSlot, out var valueBoots);
                return valueBoots?.moveSpeed + ReinforecementEquipmentStat.ReturnReinforceStat(BootsSlot, playerStatType)
                    + ReinforecementEquipmentStat.ReturnBonusStat(playerStatType) ?? 0f;
            default:
                return 0f;
        }
    }
}
