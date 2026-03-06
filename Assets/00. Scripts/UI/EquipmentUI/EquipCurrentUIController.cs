using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipCurrentUIController : UIControllerBase
{
    [Header("Binding")]
    [SerializeField] private EquipmentHandler handler;

    [SerializeField] private Button mergeButton;

    [SerializeField] private Button equipButton;

    [SerializeField] private RectTransform root;

    [SerializeField] private GameObject itemPrefab;

    [Header("Display")]
    [SerializeField] private string levelText = "Lv. 0";

    private static readonly EquipmentType[] Order =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Armor,
        EquipmentType.Glove,
        EquipmentType.Boots
    };

    private readonly Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    private readonly List<EquipItemView> views = new List<EquipItemView>();

    private EquipCurrentListView listView;
    private InventoryManager invManager;
    private Coroutine readyRoutine;
    private bool isBuilt;

    protected override void Initialize()
    {
        listView = new EquipCurrentListView(root, itemPrefab);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartReadyRoutine();
    }

    protected override void OnDisable()
    {
        if (readyRoutine != null)
        {
            StopCoroutine(readyRoutine);
            readyRoutine = null;
        }

        base.OnDisable();
    }

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += HandleRefreshRequest;
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;
        mergeButton.onClick.AddListener(ClickMerge);
        equipButton.onClick.AddListener(ClickEquip);
        BindInventory();
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= HandleRefreshRequest;
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;
        mergeButton.onClick.RemoveListener(ClickMerge);
        equipButton.onClick.RemoveListener(ClickEquip);
        UnbindInventory();
    }

    protected override void RefreshView()
    {
        BindInventory();

        if (!IsReady())
            return;

        BuildIfNeeded();
        RefreshButtons();
        RefreshCurrent();
    }

    private void ClickMerge()
    {
        handler.TryAutoMerge();
        RefreshButtons();
    }

    private void ClickEquip()
    {
        handler.TryAutoEquip();
        RefreshButtons();
    }

    private void HandleRefreshRequest()
    {
        RefreshView();
    }

    private void HandleEquippedChanged(EquipmentType equipmentType, int itemId)
    {
        if (!IsReady())
            return;

        RefreshCurrent();
    }

    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (!IsEquipType(item.ItemType))
            return;

        RefreshButtons();
    }

    private void BuildIfNeeded()
    {
        if (isBuilt)
            return;

        List<EquipItemView> builtViews = listView.Build(Order);
        views.Clear();
        for (int i = 0; i < builtViews.Count; i++)
            views.Add(builtViews[i]);

        isBuilt = true;
    }

    private void RefreshButtons()
    {
        // 버튼 활성화는 도메인 조건만 조회해서 갱신한다.
        mergeButton.interactable = handler.CanAutoMerge();
        equipButton.interactable = handler.CanAutoEquip();
    }

    private void RefreshCurrent()
    {
        if (!handler.TryGetPlayerEquipment(out PlayerEquipment player))
            return;
        if (views.Count < Order.Length)
            return;

        for (int i = 0; i < Order.Length; i++)
        {
            EquipmentType type = Order[i];
            int itemId = player.ReturnItemNum(type);
            if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
                continue;

            string iconKey = string.IsNullOrEmpty(info.iconResource)
                ? info.equipmentName
                : info.iconResource;

            Sprite icon = GetIcon(iconKey);
            int starCount = GetStarCount(info.grade);
            Color tierColor = RarityColor.TierColorByTier(info.grade);
            Color orderColor = RarityColor.ItemGradeColor(info.rarityType);

            // 현재 장착 슬롯 UI는 장비 정보 표시만 담당한다.
            views[i].Render(icon, levelText, starCount, tierColor);
            views[i].SetFrameColor(orderColor);
        }
    }

    private Sprite GetIcon(string key)
    {
        if (string.IsNullOrEmpty(key))
            return null;

        if (iconCache.TryGetValue(key, out Sprite cached))
            return cached;

        Sprite icon = Resources.Load<Sprite>(key);
        iconCache[key] = icon;
        return icon;
    }

    private static int GetStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
    }

    private void StartReadyRoutine()
    {
        if (readyRoutine != null)
            StopCoroutine(readyRoutine);

        readyRoutine = StartCoroutine(WaitReady());
    }

    private IEnumerator WaitReady()
    {
        yield return new WaitUntil(IsReady);
        RefreshView();
        readyRoutine = null;
    }

    private bool IsReady()
    {
        if (handler == null || !handler.dataLoad)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (InventoryManager.Instance == null)
            return false;

        EquipmentInventoryModule module = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        return module != null && module.IsInitialized;
    }

    private void BindInventory()
    {
        InventoryManager manager = InventoryManager.Instance;
        if (manager == null || invManager == manager)
            return;

        UnbindInventory();
        invManager = manager;
        invManager.OnItemAmountChanged += HandleAmountChanged;
    }

    private void UnbindInventory()
    {
        if (invManager == null)
            return;

        invManager.OnItemAmountChanged -= HandleAmountChanged;
        invManager = null;
    }

    private static bool IsEquipType(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Weapon:
            case ItemType.Helmet:
            case ItemType.Glove:
            case ItemType.Armor:
            case ItemType.Boots:
                return true;
            default:
                return false;
        }
    }
}

