using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentHandler : MonoBehaviour
{
    public static event Action EquipmentUiRefreshRequested;

    [SerializeField] private PlayerEquipment playerEquipment;

    public bool dataLoad;

    // 플레이어 장비/인벤토리 데이터를 초기 상태로 세팅한다.
    public void SetMyEquipOnStart(int weaponId, int helmetId, int glovesId, int armorId, int bootsId, Dictionary<int, int> equipCountDict)
    {
        if (playerEquipment == null)
        {
            Debug.LogWarning("[EquipmentHandler] PlayerEquipment 참조가 없습니다.");
            return;
        }

        playerEquipment.equipmentHandler = this;
        playerEquipment.OnEqipItem(weaponId);
        playerEquipment.OnEqipItem(helmetId);
        playerEquipment.OnEqipItem(glovesId);
        playerEquipment.OnEqipItem(armorId);
        playerEquipment.OnEqipItem(bootsId);

        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
        {
            Debug.LogWarning("[EquipmentHandler] InventoryManager가 없어 장비 인벤토리를 초기화할 수 없습니다.");
            return;
        }

        if (!equipmentModule.Setup(equipCountDict))
            Debug.LogWarning("[EquipmentHandler] 장비 모듈 초기화에 실패했습니다.");

        dataLoad = true;
        RaiseEquipmentUiRefreshRequested();
    }

    // 외부 시스템에서 현재 PlayerEquipment를 안전하게 가져오도록 제공한다.
    public bool TryGetPlayerEquipment(out PlayerEquipment equipment)
    {
        equipment = playerEquipment;
        return equipment != null;
    }

    public bool CanAutoMerge()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;

        // 자동 합성 가능 여부는 모듈 계산을 그대로 사용한다.
        return equipmentModule.CanAutoMerge();
    }

    public bool CanAutoEquip()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;
        if (playerEquipment == null)
            return false;

        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int bestItemId = equipmentModule.GetBestEquipmentId(type);
            int currentItemId = playerEquipment.ReturnItemNum(type);

            if (IsBetterEquipment(bestItemId, currentItemId))
                return true;
        }

        return false;
    }

    // 장비 자동 합성을 수행한다.
    public bool TryAutoMerge()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;
        if (!equipmentModule.RunAutoMerge())
            return false;

        // 합성 후 장비 관련 UI를 한 번에 갱신한다.
        RaiseEquipmentUiRefreshRequested();
        return true;
    }

    // UnityEvent 직렬화 호환용 래퍼.
    public void AutoMerge()
    {
        TryAutoMerge();
    }

    // 타입별 최상위 장비를 자동 장착한다.
    public bool TryAutoEquip()
    {
        if (!TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule))
            return false;
        if (playerEquipment == null)
            return false;

        bool changed = false;

        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int bestItemId = equipmentModule.GetBestEquipmentId(type);
            int currentItemId = playerEquipment.ReturnItemNum(type);

            if (!IsBetterEquipment(bestItemId, currentItemId))
                continue;

            playerEquipment.OnEqipItem(bestItemId);
            changed = true;
        }

        if (changed)
            RaiseEquipmentUiRefreshRequested();

        return changed;
    }

    // UnityEvent 직렬화 호환용 래퍼.
    public void AutoEquip()
    {
        TryAutoEquip();
    }

    private static bool TryGetEquipmentModule(out EquipmentInventoryModule equipmentModule)
    {
        equipmentModule = null;

        if (InventoryManager.Instance == null)
            return false;

        equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        return equipmentModule != null;
    }

    private static bool IsBetterEquipment(int candidateItemId, int currentItemId)
    {
        if (candidateItemId == 0)
            return false;
        if (currentItemId == 0)
            return true;
        if (candidateItemId == currentItemId)
            return false;
        if (!TryGetEquipInfo(candidateItemId, out EquipListTable candidateInfo))
            return false;
        if (!TryGetEquipInfo(currentItemId, out EquipListTable currentInfo))
            return true;
        if (candidateInfo.equipmentType != currentInfo.equipmentType)
            return false;

        int tierCompare = candidateInfo.equipmentTier.CompareTo(currentInfo.equipmentTier);
        if (tierCompare != 0)
            return tierCompare > 0;

        int rarityCompare = candidateInfo.rarityType.CompareTo(currentInfo.rarityType);
        if (rarityCompare != 0)
            return rarityCompare > 0;

        int gradeCompare = candidateInfo.grade.CompareTo(currentInfo.grade);
        if (gradeCompare != 0)
            return gradeCompare > 0;

        return candidateItemId > currentItemId;
    }

    private static bool TryGetEquipInfo(int itemId, out EquipListTable equipInfo)
    {
        equipInfo = null;
        if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return false;

        return DataManager.Instance.EquipListDict.TryGetValue(itemId, out equipInfo);
    }

    private static void RaiseEquipmentUiRefreshRequested()
    {
        EquipmentUiRefreshRequested?.Invoke();
    }
}
