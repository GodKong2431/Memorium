using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentHandler : MonoBehaviour
{
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerInventory playerInventory;
    [SerializeField] private Button autoMerge;
    [SerializeField] private Button autoEquip;

    public bool dataLoad;

    private IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
    }

    // 플레이어 착용 장비와 인벤토리 데이터를 시작 상태로 세팅한다.
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

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[EquipmentHandler] InventoryManager가 없어 장비 인벤토리를 초기화할 수 없습니다.");
            return;
        }

        var equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null || !equipmentModule.Setup(this, playerInventory, equipCountDict))
            Debug.LogWarning("[EquipmentHandler] 장비 모듈 초기화에 실패했습니다.");

        dataLoad = true;

        if (autoMerge != null)
        {
            autoMerge.onClick.RemoveListener(OnClickAutoMerge);
            autoMerge.onClick.AddListener(OnClickAutoMerge);
            autoMerge.interactable = false;
        }

        if (autoEquip != null)
        {
            autoEquip.onClick.RemoveListener(AutoEquip);
            autoEquip.onClick.AddListener(AutoEquip);
            autoEquip.interactable = false;
        }

        CheckAutoEquip();
        equipmentModule?.RefreshAutoMergeInteractable();
    }

    // 자동 합성 버튼 클릭 시 허브를 통해 장비 자동 합성을 수행한다.
    private void OnClickAutoMerge()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager.Instance.GetModule<EquipmentInventoryModule>()?.RunAutoMerge();
    }

    // 자동 합성 버튼 활성 상태를 외부 모듈에서 안전하게 제어한다.
    public void SetAutoMergeButtonInteractable(bool interactable)
    {
        if (autoMerge == null)
            return;

        autoMerge.interactable = interactable;
    }

    // 자동 합성 버튼의 현재 활성 상태를 반환한다.
    public bool IsAutoMergeButtonInteractable()
    {
        return autoMerge != null && autoMerge.interactable;
    }

    // 외부 시스템에서 현재 PlayerEquipment를 안전하게 가져오도록 제공한다.
    public bool TryGetPlayerEquipment(out PlayerEquipment equipment)
    {
        equipment = playerEquipment;
        return equipment != null;
    }

    // 타입별 최상위 장비를 찾아 자동 장착한다.
    public void AutoEquip()
    {
        if (InventoryManager.Instance == null || playerEquipment == null)
            return;
        var equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null)
            return;

        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int bestItemId = equipmentModule.GetBestEquipmentId(type);
            if (bestItemId == 0)
                continue;

            if (bestItemId == playerEquipment.ReturnItemNum(type))
                continue;

            playerEquipment.OnEqipItem(bestItemId);
        }

        if (autoEquip != null)
            autoEquip.interactable = false;
    }

    // 자동 장착 버튼 활성 조건을 점검한다.
    public void CheckAutoEquip()
    {
        if (autoEquip == null || autoEquip.interactable)
            return;

        if (InventoryManager.Instance == null || playerEquipment == null)
            return;
        var equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        if (equipmentModule == null)
            return;

        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int bestItemId = equipmentModule.GetBestEquipmentId(type);
            if (bestItemId == 0)
                continue;

            if (bestItemId == playerEquipment.ReturnItemNum(type))
                continue;

            autoEquip.interactable = true;
            break;
        }
    }
}
