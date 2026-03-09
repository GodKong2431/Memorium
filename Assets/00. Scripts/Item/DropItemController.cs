using UnityEngine;

/// <summary>
/// 드랍 아이템을 플레이어에게 끌어당기고, 획득 시 인벤토리에 반영한다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DropItemController : MonoBehaviour
{
    [SerializeField] private float waitBeforeMagnet = 3f;
    [SerializeField] private float magnetSpeed = 15f;
    [SerializeField] private float collectRadius = 1.5f;

    private int itemId;
    private int count;
    private ItemDropLogic.ItemCategory category;
    private float spawnTime;
    private bool isMagnetActive;
    private bool isCollected;

    public void Initialize(int itemId, int count, ItemDropLogic.ItemCategory category)
    {
        this.itemId = itemId;
        this.count = count;
        this.category = category;
        spawnTime = Time.time;
        isMagnetActive = false;
        isCollected = false;

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;
    }

    private void Update()
    {
        if (isCollected)
            return;

        if (!isMagnetActive)
        {
            if (Time.time - spawnTime >= waitBeforeMagnet)
                isMagnetActive = true;
            return;
        }

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        Vector3 toPlayer = player.transform.position - transform.position;
        if (toPlayer.magnitude <= collectRadius)
        {
            Collect();
            return;
        }

        transform.position += toPlayer.normalized * (magnetSpeed * Time.deltaTime);
    }

    private void Collect()
    {
        if (isCollected)
            return;

        isCollected = true;

        string itemName = GetItemName(itemId, category);
        //Debug.Log($"[아이템획득] ID={itemId} x{count} ({category}) {(string.IsNullOrEmpty(itemName) ? string.Empty : $"- {itemName}")}");

        bool isEquipment = category == ItemDropLogic.ItemCategory.Equipment;
        EnemyKillRewardDispatcher.RaiseItemCollected(itemId, count, isEquipment);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.AddItem(itemId, count);

        Destroy(gameObject);
    }

    private static string GetItemName(int itemId, ItemDropLogic.ItemCategory category)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad)
            return null;

        if (category == ItemDropLogic.ItemCategory.Equipment &&
            DataManager.Instance.EquipListDict != null &&
            DataManager.Instance.EquipListDict.TryGetValue(itemId, out var equip))
        {
            return equip.equipmentName;
        }

        if (DataManager.Instance.ItemInfoDict != null &&
            DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out var item))
        {
            return item.itemName;
        }

        return null;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected || !isMagnetActive)
            return;

        if (other.CompareTag("Player"))
            Collect();
    }
}
