using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 드랍 아이템 프리팹. 3초 대기 후 플레이어에게 끌어당겨진 뒤 회수.
/// 회수 시 OnEquipmentDropped/OnItemDropped 이벤트 발송 (PlayerData 구독).
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DropItemController : MonoBehaviour
{
    [SerializeField] private float waitBeforeMagnet = 3f;
    [SerializeField] private float magnetSpeed = 15f;
    [SerializeField] private float collectRadius = 1.5f;

    private int _itemId;
    private int _count;
    private ItemDropLogic.ItemCategory _category;
    private float _spawnTime;
    private bool _isMagnetActive;
    private bool _collected;

    public void Initialize(int itemId, int count, ItemDropLogic.ItemCategory category)
    {
        _itemId = itemId;
        _count = count;
        _category = category;
        _spawnTime = Time.time;
        _isMagnetActive = false;
        _collected = false;

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger) col.isTrigger = true;
    }

    private void Update()
    {
        if (_collected) return;

        if (!_isMagnetActive)
        {
            if (Time.time - _spawnTime >= waitBeforeMagnet) _isMagnetActive = true;
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        var toPlayer = player.transform.position - transform.position;
        if (toPlayer.magnitude <= collectRadius) { Collect(); return; }
        transform.position += toPlayer.normalized * (magnetSpeed * Time.deltaTime);
    }

    private void Collect()
    {
        if (_collected) return;
        _collected = true;

        string itemName = GetItemName(_itemId, _category);
        Debug.Log($"[아이템 획득] ID={_itemId} x{_count} ({_category}) {(string.IsNullOrEmpty(itemName) ? "" : $"- {itemName}")}");

        if (_category == ItemDropLogic.ItemCategory.Equipment)
        {
            EnemyKillRewardDispatcher.RaiseItemCollected(_itemId, _count, isEquipment: true);
            var inv = UnityEngine.Object.FindFirstObjectByType<PlayerInventory>();
            if (inv != null) inv.IncreaseEquipment(_itemId, _count);
        }
        else
        {
            EnemyKillRewardDispatcher.RaiseItemCollected(_itemId, _count, isEquipment: false);
            ItemInfoTable item = DataManager.Instance.ItemInfoDict[_itemId];
            int itemType = (int)item.itemType;
            CurrencyManager.Instance.AddCurrency((CurrencyType)itemType, 1);

            switch (item.itemType)
            {
                case ItemType.SkillScroll:
                    List<int> scrollValues = SkillInventoryManager.Instance.skillScrollIdToSkillIdDict.Values.ToList<int>();
                    //foreach (int value in scrollValues)
                    //{
                    //    Debug.Log($"[DropItemController] 스킬 아이디 목록 : {value}");
                    //}
                    int skillId = scrollValues[Random.Range(0, scrollValues.Count)];
                    Debug.Log($"[DropItemController] 스킬 아이디 : {skillId}");
                    SkillInventoryManager.Instance.AddSkill(skillId);
                    break;
            }
        }

        Destroy(gameObject);
    }

    private static string GetItemName(int itemId, ItemDropLogic.ItemCategory category)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad) return null;
        if (category == ItemDropLogic.ItemCategory.Equipment && DataManager.Instance.EquipListDict != null
            && DataManager.Instance.EquipListDict.TryGetValue(itemId, out var equip))
            return equip.equipmentName;
        if (DataManager.Instance.ItemInfoDict != null && DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out var item))
            return item.itemName;
        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_collected || !_isMagnetActive) return;
        if (other.CompareTag("Player")) Collect();
    }
}
