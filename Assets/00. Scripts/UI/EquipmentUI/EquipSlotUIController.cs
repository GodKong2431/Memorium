using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 축약 장착 슬롯 UI를 갱신한다.
public class EquipSlotUIController : UIControllerBase
{
    // 현재 장착 아이템 ID를 읽어올 플레이어 장비 참조다.
    [SerializeField] private PlayerEquipment player;
    // 인스펙터에서 연결한 슬롯 오브젝트 목록이다.
    [SerializeField] private List<GameObject> slots = new List<GameObject>();

    // 장착 변경 이벤트를 구독한다.
    protected override void Subscribe()
    {
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;
    }

    // 장착 변경 이벤트 구독을 해제한다.
    protected override void Unsubscribe()
    {
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;
    }

    // 슬롯 전체 UI를 갱신한다.
    protected override void RefreshView()
    {
        if (!IsReady())
            return;

        RefreshAll();
    }

    // 단일 장착 변경 이벤트를 받아 해당 슬롯을 갱신한다.
    private void HandleEquippedChanged(EquipmentType type, int itemId)
    {
        if (!IsReady())
            return;

        RefreshOne(type, itemId);
    }

    // 모든 장비 타입 슬롯을 순회하면서 갱신한다.
    private void RefreshAll()
    {
        foreach (EquipmentType type in Enum.GetValues(typeof(EquipmentType)))
        {
            int itemId = player.ReturnItemNum(type);
            RefreshOne(type, itemId);
        }
    }

    // 장비 타입/아이템 ID 기준으로 슬롯 하나를 갱신한다.
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

    // 슬롯 오브젝트의 색상과 라벨 텍스트를 반영한다.
    private static void RenderSlot(GameObject slot, EquipListTable info)
    {
        // Toggle 슬롯은 흰색 유지, 일반 슬롯은 희귀도 색상을 적용한다.
        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            if (slot.GetComponent<Toggle>() != null)
                image.color = Color.white;
            else
                image.color = RarityColor.ItemGradeColor(info.rarityType);
        }

        // 설명과 장비 이름을 한 줄 문자열로 합친다.
        string label = info.description + "\n" + info.equipmentName;

        // Legacy Text 컴포넌트가 있으면 텍스트를 반영한다.
        Text legacyText = slot.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = label;

        // TMP 텍스트 컴포넌트가 있으면 텍스트를 반영한다.
        TMP_Text tmpText = slot.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = label;
    }

    // 갱신에 필요한 런타임 데이터와 바인딩 상태를 검사한다.
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
