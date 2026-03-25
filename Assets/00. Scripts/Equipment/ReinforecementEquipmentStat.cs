using System;
using System.Collections.Generic;
using UnityEngine;

public struct WeaponReinforcement
{
    public int attackPower;
    public float attackSpeed;

    public int bonusAttackPower;
    public float bonusAttackSpeed;
}
public struct HelmetReinforcement
{
    public float defense;

    public float bonusDefense;
}
public struct GloveReinforcement
{
    public float magicDefense;

    public float bonusMaginDefense;
}
public struct ArmorReinforcement
{
    public int hp;

    public float bonusHp;
}
public struct BootsReinforcement
{
    public float moveSpeed;

    public float bonusMoveSpeed;
}


public static class ReinforecementEquipmentStat
{
    public static Dictionary<int, WeaponReinforcement> weaponReinforcement;
    public static Dictionary<int, HelmetReinforcement> helmetReinforcement;
    public static Dictionary<int, GloveReinforcement> gloveReinforcement;
    public static Dictionary<int, ArmorReinforcement> armorReinforcement;
    public static Dictionary<int, BootsReinforcement> bootsReinforcement;

    public static void Init()
    {
        if (weaponReinforcement == null)
        {
            weaponReinforcement= new Dictionary<int, WeaponReinforcement> ();
            helmetReinforcement = new Dictionary<int, HelmetReinforcement>();
            gloveReinforcement = new Dictionary<int, GloveReinforcement>();
            armorReinforcement = new Dictionary<int, ArmorReinforcement>();
            bootsReinforcement = new Dictionary<int, BootsReinforcement>();
            foreach (var equip in DataManager.Instance.EquipListDict)
            {
                switch (equip.Value.equipmentType)
                {
                    case EquipmentType.Weapon:
                        weaponReinforcement[equip.Key] = new WeaponReinforcement();
                        break;
                    case EquipmentType.Helmet:
                        helmetReinforcement[equip.Key] = new HelmetReinforcement();
                        break;
                    case EquipmentType.Glove:
                        gloveReinforcement[equip.Key] = new GloveReinforcement();
                        break;
                    case EquipmentType.Armor:
                        armorReinforcement[equip.Key] = new ArmorReinforcement();
                        break;
                    case EquipmentType.Boots:
                        bootsReinforcement[equip.Key] = new BootsReinforcement();
                        break;
                }
            }
        }
    }
    public static void InitReinforcement(Dictionary<int, EquipmentData> unlockEquipmentDict)
    {
        foreach (var equip in unlockEquipmentDict)
        {
            if (weaponReinforcement == null)
                Init();
            SetReinforcement(equip.Key, equip.Value.equipmentReinforcement);
        }
    }
    public static void SetReinforcement(int itemId, int equipmentReinforcement)
    {
        int Stat1Id = DataManager.Instance.EquipListDict[itemId].statType1;
        int Stat2Id = DataManager.Instance.EquipListDict[itemId].statType2;

        //스탯 성장치 <- 테이블에서 불러와라
        float stat_01 = DataManager.Instance.EquipStatsDict[Stat1Id].statPerLevel;
        float stat_02 = 0f;

        stat_01 *= equipmentReinforcement;

        if (Stat2Id != 0)
        {
            stat_02 = DataManager.Instance.EquipStatsDict[Stat2Id].statPerLevel;
            stat_02 *= equipmentReinforcement;
        }

        if (weaponReinforcement == null)
            Init();

        EquipmentType type;

        if (!ReturnItemIdToEquipmentType(itemId, out type))
            return;

        //Debug.Log($"[ReinforecementEquipmentStat] 강화수치 조정 아이템 아이디 : {itemId}");

        {
            switch (type)
            {
                case EquipmentType.Weapon:
                    WeaponReinforcement weapon = weaponReinforcement[itemId];
                    weapon.attackPower = (int)stat_01;
                    weapon.attackSpeed = stat_02;
                    weaponReinforcement[itemId] = weapon;
                    break;
                case EquipmentType.Helmet:
                    HelmetReinforcement helmet = helmetReinforcement[itemId];
                    helmet.defense = stat_01;
                    helmetReinforcement[itemId] = helmet;
                    break;
                case EquipmentType.Glove:
                    GloveReinforcement glove = gloveReinforcement[itemId];
                    glove.magicDefense = stat_01;
                    gloveReinforcement[itemId] = glove;
                    break;
                case EquipmentType.Armor:
                    ArmorReinforcement armor = armorReinforcement[itemId];
                    armor.hp = (int)stat_01;
                    armorReinforcement[itemId] = armor;
                    break;
                case EquipmentType.Boots:
                    BootsReinforcement boots = bootsReinforcement[itemId];
                    boots.moveSpeed = stat_01;
                    bootsReinforcement[itemId] = boots;
                    break;
            }
        }
    }

    public static bool ReturnItemIdToEquipmentType(int itemId, out EquipmentType type)
    {

        if (DataManager.Instance.EquipListDict.TryGetValue(itemId, out var equipment))
        {
            type=equipment.equipmentType;
            return true;
        }
        else
        {
            type = EquipmentType.Weapon;
            return false;
        }
    }

    //저장된 세이브 데이터에서 보너스 스탯 가져오는 용도
    public static void InitBonusStat(Dictionary<int, EquipmentData> unlockEquipmentDict)
    {
        foreach (var equip in unlockEquipmentDict)
        {
            if (weaponReinforcement == null)
                Init();
            SetBonusStat(equip.Key,equip.Value.equipmentReinforcement);
        }
    }

    public static bool SetBonusStat(int equipmentId, int equipmentReinforcement)
    {
        bool checkGetCurrecyStat=false;

        EquipListTable equipment = DataManager.Instance.EquipListDict[equipmentId];
        int statId_01 = equipment.statType1;
        int statId_02 = equipment.statType2;


        EquipStatsTable stat_01 = DataManager.Instance.EquipStatsDict[statId_01];
        EquipStatsTable stat_02 = null;

        //보너스 스탯 획득 강화 수치를 만족하지 못하면 반환
        if (statId_02 <= 0)
        {
            if (stat_01.bonusStatPerLevel > equipmentReinforcement)
                return false;

            if(equipmentReinforcement % (int)stat_01.bonusStatPerLevel==0)
                checkGetCurrecyStat=true;
        }
        else
        {
            stat_02 = DataManager.Instance.EquipStatsDict[statId_02];
            if (stat_01.bonusStatPerLevel > equipmentReinforcement
                && stat_02.bonusStatPerLevel > equipmentReinforcement)
                return false;

            if (equipmentReinforcement % (int)stat_01.bonusStatPerLevel == 0
                || equipmentReinforcement % stat_02.bonusStatPerLevel==0)
                checkGetCurrecyStat = true;
        }

        EquipmentType type;
        if (!ReturnItemIdToEquipmentType(equipmentId, out type))
            return false;

        //보너스 스탯 단계
        int bonusStatStep_01 = equipmentReinforcement / (int)stat_01.bonusStatPerLevel;
        int bonusStatStep_02 = 0;
        if (stat_02 != null)
        {
            bonusStatStep_02 = equipmentReinforcement / (int)stat_02.bonusStatPerLevel;
        }

        Debug.Log($"[ReinforecementEquipmentStat] 강화 보너스 수치 조정 아이템 아이디 : {equipmentId} 및 보너스 수치 단계 {bonusStatStep_01}");
        switch (type)
        {
            case EquipmentType.Weapon:
                WeaponReinforcement weapon = weaponReinforcement[equipmentId];
                float baseBonusWeaponStat = (float)(stat_01.baseBonusStat * Math.Pow(stat_01.bonusStatPerTier, equipment.equipmentTier - 1));
                Debug.Log($"[ReinforecementEquipmentStat] 강화 보너스수치 조정 아이템 아이디 : {equipmentId} 및 초기 보너스 스탯 {baseBonusWeaponStat}");
                Debug.Log($"[ReinforecementEquipmentStat] 강화 보너스수치 조정 아이템 아이디 : {equipmentId} 초기 스탯 {stat_01.baseBonusStat} 티어별 증가량 {stat_01.bonusStatPerTier} 티어 {equipment.equipmentTier - 1}");
                weapon.bonusAttackPower = 0;
                while (bonusStatStep_01 > 0)
                {

                    weapon.bonusAttackPower += (int)baseBonusWeaponStat;
                    Debug.Log($"[ReinforecementEquipmentStat] 강화 보너스수치 조정 아이템 아이디 : {equipmentId} 및 보너스 스탯 상태 {weapon.bonusAttackPower} 강화단계당 증가량{stat_01.bonusStatPerStep}");
                    bonusStatStep_01--;
                    baseBonusWeaponStat *= stat_01.bonusStatPerStep;
                }
                weapon.bonusAttackSpeed = 0;
                baseBonusWeaponStat = (float)(stat_02.baseBonusStat * Math.Pow(stat_02.bonusStatPerTier, equipment.equipmentTier - 1));
                Debug.Log($"[ReinforecementEquipmentStat] 강화 스피드 보너스수치 조정 아이템 아이디 : {equipmentId} 및 초기 보너스 스탯 {baseBonusWeaponStat}");
                Debug.Log($"[ReinforecementEquipmentStat] 강화 스피드 보너스수치 조정 아이템 아이디 : {equipmentId} 초기 스탯 {stat_02.baseBonusStat} 티어별 증가량 {stat_02.bonusStatPerTier} 티어 {equipment.equipmentTier - 1}");
                while (bonusStatStep_02 > 0)
                {
                    weapon.bonusAttackSpeed += baseBonusWeaponStat;
                    Debug.Log($"[ReinforecementEquipmentStat] 강화 보너스수치 조정 아이템 아이디 : {equipmentId} 및 보너스 스탯 상태 {weapon.bonusAttackSpeed} 강화단계당 증가량{stat_02.bonusStatPerStep}");
                    bonusStatStep_02--;
                    baseBonusWeaponStat *= stat_02.bonusStatPerStep;
                }
                weaponReinforcement[equipmentId] = weapon;
                break;
            case EquipmentType.Helmet:
                HelmetReinforcement helmet = helmetReinforcement[equipmentId];
                float baseBonusHelmetStat = (float)(stat_01.baseBonusStat * Math.Pow(stat_01.bonusStatPerTier, equipment.equipmentTier - 1));
                helmet.bonusDefense = 0;
                while (bonusStatStep_01 > 0)
                {
                    helmet.bonusDefense += baseBonusHelmetStat;
                    bonusStatStep_01--;
                    baseBonusHelmetStat *= stat_01.bonusStatPerStep;
                }
                helmetReinforcement[equipmentId] = helmet;
                break;
            case EquipmentType.Glove:
                GloveReinforcement glove = gloveReinforcement[equipmentId];
                float baseBonusGloveStat = (float)(stat_01.baseBonusStat * Math.Pow(stat_01.bonusStatPerTier, equipment.equipmentTier - 1));
                glove.bonusMaginDefense = 0;
                while (bonusStatStep_01 > 0)
                {
                    glove.magicDefense += baseBonusGloveStat;
                    bonusStatStep_01--;
                    baseBonusGloveStat *= stat_01.bonusStatPerStep;
                }
                gloveReinforcement[equipmentId] = glove;
                break;
            case EquipmentType.Armor:
                ArmorReinforcement armor = armorReinforcement[equipmentId];
                float baseBonusArmorStat = (float)(stat_01.baseBonusStat * Math.Pow(stat_01.bonusStatPerTier, equipment.equipmentTier - 1));
                armor.bonusHp = 0;
                while (bonusStatStep_01 > 0)
                {
                    armor.bonusHp += (int)baseBonusArmorStat;
                    bonusStatStep_01--;
                    baseBonusArmorStat *= stat_01.bonusStatPerStep;
                }
                armorReinforcement[equipmentId] = armor;
                break;
            case EquipmentType.Boots:
                BootsReinforcement boots = bootsReinforcement[equipmentId];
                float baseBonusBootsStat = (float)(stat_01.baseBonusStat * Math.Pow(stat_01.bonusStatPerTier, equipment.equipmentTier - 1));
                boots.bonusMoveSpeed = 0;
                while (bonusStatStep_01 > 0)
                {
                    boots.bonusMoveSpeed += baseBonusBootsStat;
                    bonusStatStep_01--;
                    baseBonusBootsStat *= stat_01.bonusStatPerStep;
                }
                bootsReinforcement[equipmentId] = boots;
                break;
        }

        return checkGetCurrecyStat;
    }
    public static float ReturnBonusStat(StatType type)
    {
        if (weaponReinforcement == null)
            Init();
        switch (type)
        {
            case StatType.ATK:
                int totalAttackBonus = 0;
                foreach (var weapon in weaponReinforcement)
                    totalAttackBonus += weapon.Value.bonusAttackPower;
                return totalAttackBonus;
            case StatType.ATK_SPEED:
                float totalAttackSpeed = 0;
                foreach (var weapon in weaponReinforcement)
                    totalAttackSpeed += weapon.Value.bonusAttackSpeed;
                return totalAttackSpeed;
            case StatType.PHYS_DEF:
                float totalDefense = 0;
                foreach (var helmet in helmetReinforcement)
                    totalDefense += helmet.Value.bonusDefense;
                return totalDefense;
            case StatType.MAGIC_DEF:
                float totalMagicDefense = 0;
                foreach (var glove in gloveReinforcement)
                    totalMagicDefense += glove.Value.bonusMaginDefense;
                return totalMagicDefense;
            case StatType.HP:
                float totalHp = 0;
                foreach (var armor in armorReinforcement)
                    totalHp += armor.Value.bonusHp;
                return totalHp;
            case StatType.MOVE_SPEED:
                float totalMoveSpeed = 0;
                foreach (var boots in bootsReinforcement)
                    totalMoveSpeed += boots.Value.bonusMoveSpeed;
                return totalMoveSpeed;
            default:
                return 0;
        }
    }

    public static float ReturnReinforceStat(int id, StatType type)
    {
        if (weaponReinforcement == null)
            Init();
        switch (type)
        {
            case StatType.ATK:
                if(!weaponReinforcement.ContainsKey(id))
                    return 0;
                return weaponReinforcement[id].attackPower;
            case StatType.ATK_SPEED:
                if (!weaponReinforcement.ContainsKey(id))
                    return 0;
                return weaponReinforcement[id].attackSpeed;
            case StatType.PHYS_DEF:
                if (!helmetReinforcement.ContainsKey(id))
                    return 0;
                return helmetReinforcement[id].defense;
            case StatType.MAGIC_DEF:
                if (!gloveReinforcement.ContainsKey(id))
                    return 0;
                return gloveReinforcement[id].magicDefense;
            case StatType.HP:
                if (!armorReinforcement.ContainsKey(id))
                    return 0;
                return armorReinforcement[id].hp;
            case StatType.MOVE_SPEED:
                if (!bootsReinforcement.ContainsKey(id))
                    return 0;
                return bootsReinforcement[id].moveSpeed;
            default:
                return 0;
        }
    }
}
