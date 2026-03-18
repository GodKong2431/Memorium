using UnityEngine;

/// <summary>
/// 드랍 아이템을 플레이어에게 끌어당기고, 획득 시 인벤토리에 반영한다.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
public class DropItemController : MonoBehaviour
{
    [SerializeField] private float waitBeforeMagnet = 3f;
    [SerializeField] private float magnetSpeed = 15f;
    [SerializeField] private float magnetAcceleration = 25f;
    [SerializeField] private float maxMagnetSpeed = 80f;
    [SerializeField] private float collectRadius = 1.5f;

    [Header("Drop VFX (optional)")]
    [SerializeField] private GameObject defaultDropVfxPrefab;
    [SerializeField] private GameObject equipmentCommonDropVfxPrefab;
    [SerializeField] private GameObject equipmentUncommonDropVfxPrefab;
    [SerializeField] private GameObject equipmentEpicDropVfxPrefab;
    [SerializeField] private GameObject equipmentLegendaryDropVfxPrefab;
    [SerializeField] private GameObject equipmentMythicDropVfxPrefab;

    private int itemId;
    private int count;
    private ItemDropLogic.ItemCategory category;
    private float spawnTime;
    private float magnetActivatedTime;
    private bool isMagnetActive;
    private bool isCollected;
    private GameObject spawnedVfx;

    public void Initialize(int itemId, int count, ItemDropLogic.ItemCategory category)
    {
        this.itemId = itemId;
        this.count = count;
        this.category = category;
        spawnTime = Time.time;
        magnetActivatedTime = 0f;
        isMagnetActive = false;
        isCollected = false;

        var col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        EnsureDropVfxSpawned();

        //스테이지 클리어 혹은 실패 시 현존 아이템 모두 획득
        StageManager.Instance.OnStageClearOrFailed += Collect;
    }

    private void EnsureDropVfxSpawned()
    {
        if (spawnedVfx != null)
            return;

        var prefab = ResolveVfxPrefab();
        if (prefab == null)
            return;

        spawnedVfx = Instantiate(prefab, transform);
        spawnedVfx.transform.localPosition = Vector3.zero;
        spawnedVfx.transform.localRotation = Quaternion.identity;
        spawnedVfx.transform.localScale = Vector3.one;
    }

    private GameObject ResolveVfxPrefab()
    {
        if (category != ItemDropLogic.ItemCategory.Equipment)
            return defaultDropVfxPrefab;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return defaultDropVfxPrefab;

        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out var equip) || equip == null)
            return defaultDropVfxPrefab;

        return equip.rarityType switch
        {
            RarityType.normal => equipmentCommonDropVfxPrefab != null ? equipmentCommonDropVfxPrefab : defaultDropVfxPrefab,
            RarityType.uncommon => equipmentUncommonDropVfxPrefab != null ? equipmentUncommonDropVfxPrefab : defaultDropVfxPrefab,
            RarityType.rare => equipmentEpicDropVfxPrefab != null ? equipmentEpicDropVfxPrefab : defaultDropVfxPrefab,
            RarityType.legendary => equipmentLegendaryDropVfxPrefab != null ? equipmentLegendaryDropVfxPrefab : defaultDropVfxPrefab,
            RarityType.mythic => equipmentMythicDropVfxPrefab != null ? equipmentMythicDropVfxPrefab : defaultDropVfxPrefab,
            _ => defaultDropVfxPrefab,
        };
    }

    private void Update()
    {
        if (isCollected)
            return;

        if (!isMagnetActive)
        {
            if (Time.time - spawnTime >= waitBeforeMagnet)
            {
                isMagnetActive = true;
                magnetActivatedTime = Time.time;
            }
            return;
        }

        if (!ScenePlayerLocator.TryGetPlayerTransform(out Transform playerTransform))
            return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;
        if (distance <= collectRadius)
        {
            Collect();
            return;
        }

        float t = Mathf.Max(0f, Time.time - magnetActivatedTime);
        float speed = Mathf.Min(maxMagnetSpeed, magnetSpeed + magnetAcceleration * t);
        float step = speed * Time.deltaTime;

        if (step >= distance)
        {
            Collect();
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, playerTransform.position, step);
    }

    private void Collect()
    {
        if (isCollected)
            return;

        isCollected = true;

        string itemName = GetItemName(itemId, category);


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

    private void OnDisable()
    {
        Collect();
    }
}
