using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private GameObject prefabEquipTierGroup; // 장비 티어 그룹 UI 프리팹.
    [SerializeField] private Transform[] uIPage; // 장비 타입별 UI 루트.
    [SerializeField] private Text testInventory; // 디버그용 인벤토리 텍스트.

    private readonly Dictionary<int, EquipmentSlotComponent> slotByItemId = new Dictionary<int, EquipmentSlotComponent>(); // 장비 ID와 슬롯 컴포넌트 매핑.
    private readonly List<GameObject> spawnedGroups = new List<GameObject>(); // 런타임에 생성된 장비 그룹 UI 목록.

    // 장비 목록과 보유 수량으로 장비 UI를 새로 구성한다.
    public void BuildEquipmentInventory(IReadOnlyList<int> sortedEquipmentItemIds, IReadOnlyDictionary<int, int> countByItemId)
    {
        ClearSpawnedGroups();
        slotByItemId.Clear();

        if (sortedEquipmentItemIds == null || sortedEquipmentItemIds.Count == 0)
            return;
        if (DataManager.Instance == null || DataManager.Instance.EquipListDict == null)
            return;
        if (prefabEquipTierGroup == null || uIPage == null || uIPage.Length == 0)
            return;

        EquipmentSlotContainer templateContainer = prefabEquipTierGroup.GetComponent<EquipmentSlotContainer>();
        if (templateContainer == null || templateContainer.slot == null || templateContainer.slot.Count == 0)
            return;

        int groupSize = templateContainer.slot.Count;
        for (int i = 0; i < sortedEquipmentItemIds.Count; i += groupSize)
        {
            int itemId = sortedEquipmentItemIds[i];
            int equipType = itemId / 10000 % 10;
            int pageIndex = equipType - 1;
            if (pageIndex < 0 || pageIndex >= uIPage.Length || uIPage[pageIndex] == null)
                continue;

            GameObject groupObject = Instantiate(prefabEquipTierGroup, uIPage[pageIndex]);
            spawnedGroups.Add(groupObject);

            EquipmentSlotContainer slotContainer = groupObject.GetComponent<EquipmentSlotContainer>();
            if (slotContainer == null || slotContainer.slot == null || slotContainer.slot.Count == 0)
                continue;

            int maxIndex = Mathf.Min(i + groupSize, sortedEquipmentItemIds.Count);
            for (int j = i; j < maxIndex; j++)
            {
                int localIndex = j - i;
                if (localIndex < 0 || localIndex >= slotContainer.slot.Count)
                    break;

                int currentItemId = sortedEquipmentItemIds[j];
                if (!DataManager.Instance.EquipListDict.TryGetValue(currentItemId, out var equipInfo))
                    continue;

                if (string.IsNullOrEmpty(slotContainer.gradeText.text))
                    slotContainer.gradeText.text = equipInfo.grade.ToString();

                EquipmentSlotComponent slotComponent = slotContainer.slot[localIndex];
                slotByItemId[currentItemId] = slotComponent;

                int ownedCount = 0;
                if (countByItemId != null)
                    countByItemId.TryGetValue(currentItemId, out ownedCount);

                ApplySlotOwned(slotComponent, ownedCount > 0);
                ApplySlotCount(slotComponent, ownedCount);
            }
        }

        ShowInventoryText(countByItemId);
    }

    // 장비 하나의 보유 수량을 갱신한다.
    public void UpdateEquipmentCount(int itemId, int count)
    {
        if (!slotByItemId.TryGetValue(itemId, out var slotComponent) || slotComponent == null)
            return;

        int clamped = Mathf.Max(0, count);
        ApplySlotOwned(slotComponent, clamped > 0);
        ApplySlotCount(slotComponent, clamped);
    }

    // 인벤토리 디버그 텍스트를 최신 상태로 갱신한다.
    public void ShowInventoryText(IReadOnlyDictionary<int, int> countByItemId)
    {
        if (testInventory == null)
            return;

        if (countByItemId == null || countByItemId.Count == 0)
        {
            testInventory.text = string.Empty;
            return;
        }

        StringBuilder builder = new StringBuilder();
        foreach (var pair in countByItemId)
            builder.AppendLine($"ID : {pair.Key} 갯수 : {pair.Value}");

        testInventory.text = builder.ToString();
    }

    // 이전에 생성한 장비 그룹 UI를 제거한다.
    private void ClearSpawnedGroups()
    {
        for (int i = 0; i < spawnedGroups.Count; i++)
        {
            if (spawnedGroups[i] == null)
                continue;

            Destroy(spawnedGroups[i]);
        }

        spawnedGroups.Clear();
    }

    // 슬롯의 보유 여부 표시를 갱신한다.
    private static void ApplySlotOwned(EquipmentSlotComponent slotComponent, bool isOwned)
    {
        if (slotComponent.ownerShipImage != null)
            slotComponent.ownerShipImage.SetActive(!isOwned);
    }

    // 슬롯의 수량 UI를 갱신한다.
    private static void ApplySlotCount(EquipmentSlotComponent slotComponent, int count)
    {
        int clamped = Mathf.Max(0, count);

        if (slotComponent.equipmentCountSlider != null)
            slotComponent.equipmentCountSlider.value = clamped;
        if (slotComponent.equipmentCountText != null)
            slotComponent.equipmentCountText.text = FormatCountText(clamped);
    }

    // 장비 수량 표기 형식을 통일한다.
    private static string FormatCountText(int count)
    {
        string itemCount = count > 99 ? "99+" : count.ToString();
        return itemCount + " / 3";
    }
}
