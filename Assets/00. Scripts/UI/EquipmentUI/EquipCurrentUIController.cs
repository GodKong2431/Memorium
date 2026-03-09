using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipCurrentUIController : UIControllerBase
{
    [SerializeField] private EquipmentHandler handler;
    [SerializeField] private Button mergeButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private RectTransform root;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private string levelText = "Lv. 0";

    private static readonly EquipmentType[] Order =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Armor,
        EquipmentType.Glove,
        EquipmentType.Boots
    };

    private readonly List<EquipItemView> views = new List<EquipItemView>();

    private InventoryManager inventory;
    private bool isBuilt;

    private void Update()
    {
        if (!isBuilt)
            RefreshView();
    }

    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;

        if (mergeButton != null)
            mergeButton.onClick.AddListener(ClickMerge);

        if (equipButton != null)
            equipButton.onClick.AddListener(ClickEquip);

        BindInventory();
    }

    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;

        if (mergeButton != null)
            mergeButton.onClick.RemoveListener(ClickMerge);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(ClickEquip);

        UnbindInventory();
    }

    protected override void RefreshView()
    {
        if (!TryPrepare())
            return;

        if (!isBuilt)
            BuildViews();

        RefreshButtons();
        RefreshCurrent();
    }

    private bool TryPrepare()
    {
        if (handler == null || !handler.dataLoad)
            return false;

        if (root == null || itemPrefab == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (!BindInventory())
            return false;

        EquipmentInventoryModule module = inventory.GetModule<EquipmentInventoryModule>();
        return module != null && module.IsInitialized;
    }

    private bool BindInventory()
    {
        InventoryManager current = InventoryManager.Instance;
        if (current == null)
            return false;

        if (inventory == current)
            return true;

        UnbindInventory();
        inventory = current;
        inventory.OnItemAmountChanged += HandleAmountChanged;
        return true;
    }

    private void UnbindInventory()
    {
        if (inventory == null)
            return;

        inventory.OnItemAmountChanged -= HandleAmountChanged;
        inventory = null;
    }

    private void BuildViews()
    {
        views.Clear();

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        for (int i = 0; i < Order.Length; i++)
        {
            GameObject go = Instantiate(itemPrefab, root, false);
            go.name = $"CurrentEquipment_{Order[i]}";

            EquipItemUI ui = go.GetComponent<EquipItemUI>();
            if (ui == null)
                continue;

            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = false;
            ui.MergeSlider.gameObject.SetActive(false);

            views.Add(new EquipItemView(ui));
        }

        isBuilt = true;
    }

    private void RefreshButtons()
    {
        if (mergeButton != null)
            mergeButton.interactable = handler.CanAutoMerge();

        if (equipButton != null)
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
            int itemId = player.ReturnItemNum(Order[i]);
            if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
                continue;

            Sprite icon = LoadIcon(info);
            int starCount = GetStarCount(info.grade);

            views[i].Render(icon, levelText, starCount, RarityColor.TierColorByTier(info.grade));
            views[i].SetFrameColor(RarityColor.ItemGradeColor(info.rarityType));
        }
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

    private void HandleEquippedChanged(EquipmentType type, int itemId)
    {
        RefreshCurrent();
    }

    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (IsEquipmentItem(item.ItemType))
            RefreshButtons();
    }

    private static Sprite LoadIcon(EquipListTable table)
    {
        string key = string.IsNullOrEmpty(table.iconResource)
            ? table.equipmentName
            : table.iconResource;

        return string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>(key);
    }

    private static int GetStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
    }

    private static bool IsEquipmentItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:
            case ItemType.Helmet:
            case ItemType.Armor:
            case ItemType.Glove:
            case ItemType.Boots:
                return true;
            default:
                return false;
        }
    }
}
