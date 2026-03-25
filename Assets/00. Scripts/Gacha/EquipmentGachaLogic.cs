using System.Linq;
using UnityEngine;

/// <summary>
/// 장비(무기/방어구) 가챠 뽑기 로직.
/// </summary>
public static class EquipmentGachaLogic
{
    private static readonly EquipmentType[] ArmorTypes = { EquipmentType.Helmet, EquipmentType.Boots, EquipmentType.Glove, EquipmentType.Armor };

    /// <summary>오프셋 인덱스 3, 4 (4%, 1%) = 대박 아이템</summary>
    private const int RareOffsetIndexStart = 3;

    /// <summary>1회 뽑기 결과 (아이템 ID, 대박 여부)</summary>
    public struct DrawResult
    {
        public int ItemId;
        public bool IsRare;
        public bool IsHighestTier;
    }

    /// <summary>무기 1회 뽑기.</summary>
    public static DrawResult DrawWeapon(int stage, bool forceHighestTier = false)
    {
        int offsetIndex = forceHighestTier ? GachaConfig.Offsets.Length - 1 : RollOffsetIndex();
        int combatPower = GachaConfig.GetBaseCombatPower(stage) + GachaConfig.Offsets[offsetIndex];
        int tier = GachaConfig.CombatPowerToTier(combatPower);
        int itemId = GetEquipmentId(EquipmentType.Weapon, tier);
        bool isHighestTier = tier >= GachaConfig.GetMaxTierForStage(stage);
        return new DrawResult { ItemId = itemId, IsRare = offsetIndex >= RareOffsetIndexStart, IsHighestTier = isHighestTier };
    }

    /// <summary>방어구 1회 뽑기. 4종류 25% 확정 후 무기 뽑기와 동일 로직.</summary>
    public static DrawResult DrawArmor(int stage, bool forceHighestTier = false)
    {
        EquipmentType armorType = ArmorTypes[Random.Range(0, ArmorTypes.Length)];
        int offsetIndex = forceHighestTier ? GachaConfig.Offsets.Length - 1 : RollOffsetIndex();
        int combatPower = GachaConfig.GetBaseCombatPower(stage) + GachaConfig.Offsets[offsetIndex];
        int tier = GachaConfig.CombatPowerToTier(combatPower);
        int itemId = GetEquipmentId(armorType, tier);
        bool isHighestTier = tier >= GachaConfig.GetMaxTierForStage(stage);
        return new DrawResult { ItemId = itemId, IsRare = offsetIndex >= RareOffsetIndexStart, IsHighestTier = isHighestTier };
    }

    /// <summary>오프셋 인덱스 확률 롤 (60/25/10/4/1). 낮은 인덱스=일반, 높은 인덱스=대박!</summary>
    private static int RollOffsetIndex()
    {
        float r = Random.value;
        float sum = 0f;
        for (int i = 0; i < GachaConfig.OffsetProbabilities.Length; i++)
        {
            sum += GachaConfig.OffsetProbabilities[i];
            if (r < sum) return i;
        }
        return GachaConfig.OffsetProbabilities.Length - 1;
    }

    /// <summary>타입+티어로 EquipListDict에서 ID 조회</summary>
    private static int GetEquipmentId(EquipmentType equipmentType, int tier)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return 0;

        var match = DataManager.Instance.EquipListDict.Values
            .FirstOrDefault(e => e.equipmentType == equipmentType && e.equipmentTier == tier);
        return match != null ? match.ID : 0;
    }
}
