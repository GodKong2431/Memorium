using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class CachaUIController : UIControllerBase
{
    [Header("Sub Menu")]
    [SerializeField] private Toggle toggleWeapon;
    [SerializeField] private Toggle toggleArmor;
    [SerializeField] private Toggle toggleSkillScroll;
    [SerializeField] private GachaType initialSubMenu = GachaType.Weapon;

    [Header("Summon List")]
    [SerializeField] private RectTransform summonItemContentRoot;
    [SerializeField] private GameObject summonItemPrefab;

    [Header("Popup")]
    [SerializeField] private CachaCrystalChangePopupUI crystalChangePopup;
    [SerializeField] private CachaResultPopupUI resultPopup;

    private GachaType currentSubMenu;
    private readonly List<CachaSummonItemUI> summonItemUIs = new List<CachaSummonItemUI>();

    protected override void Initialize()
    {
        currentSubMenu = SanitizeSubMenu(initialSubMenu);
    }

    protected override void Subscribe()
    {
        if (toggleWeapon != null)
            toggleWeapon.onValueChanged.AddListener(OnWeaponToggleChanged);

        if (toggleArmor != null)
            toggleArmor.onValueChanged.AddListener(OnArmorToggleChanged);

        if (toggleSkillScroll != null)
            toggleSkillScroll.onValueChanged.AddListener(OnSkillScrollToggleChanged);

        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
            inventory.OnItemAmountChanged += OnItemAmountChanged;
    }

    protected override void Unsubscribe()
    {
        if (toggleWeapon != null)
            toggleWeapon.onValueChanged.RemoveListener(OnWeaponToggleChanged);

        if (toggleArmor != null)
            toggleArmor.onValueChanged.RemoveListener(OnArmorToggleChanged);

        if (toggleSkillScroll != null)
            toggleSkillScroll.onValueChanged.RemoveListener(OnSkillScrollToggleChanged);

        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;

        InventoryManager inventory = InventoryManager.Instance;
        if (inventory != null)
            inventory.OnItemAmountChanged -= OnItemAmountChanged;

        if (crystalChangePopup != null)
            crystalChangePopup.Hide();

        if (resultPopup != null)
            resultPopup.Hide();
    }

    protected override void RefreshView()
    {
        SyncSubMenuToggleState();

        if (crystalChangePopup != null)
            crystalChangePopup.Hide();

        if (resultPopup != null)
            resultPopup.Hide();

        if (summonItemContentRoot == null || summonItemPrefab == null)
            return;

        RebuildSummonItems(currentSubMenu);
    }

    private void OnWeaponToggleChanged(bool isOn)
    {
        if (isOn)
            SwitchSubMenu(GachaType.Weapon);
    }

    private void OnArmorToggleChanged(bool isOn)
    {
        if (isOn)
            SwitchSubMenu(GachaType.Armor);
    }

    private void OnSkillScrollToggleChanged(bool isOn)
    {
        if (isOn)
            SwitchSubMenu(GachaType.SkillScroll);
    }

    private void SwitchSubMenu(GachaType gachaType)
    {
        GachaType nextSubMenu = SanitizeSubMenu(gachaType);
        if (currentSubMenu == nextSubMenu)
        {
            SyncSubMenuToggleState();
            return;
        }

        currentSubMenu = nextSubMenu;
        RefreshView();
    }

    private void SyncSubMenuToggleState()
    {
        if (toggleWeapon != null)
            toggleWeapon.SetIsOnWithoutNotify(currentSubMenu == GachaType.Weapon);

        if (toggleArmor != null)
            toggleArmor.SetIsOnWithoutNotify(currentSubMenu == GachaType.Armor);

        if (toggleSkillScroll != null)
            toggleSkillScroll.SetIsOnWithoutNotify(currentSubMenu == GachaType.SkillScroll);
    }

    private void RebuildSummonItems(GachaType gachaType)
    {
        ClearSummonItems();

        int summonItemCount = GetSummonItemCount(gachaType);
        for (int i = 0; i < summonItemCount; i++)
        {
            Object summonItemObject = Instantiate((Object)summonItemPrefab, summonItemContentRoot, false);
            GameObject summonItemInstance = summonItemObject as GameObject;
            if (summonItemInstance == null && summonItemObject is Component component)
                summonItemInstance = component.gameObject;

            if (summonItemInstance == null)
            {
                Debug.LogError($"[CachaUIController] Summon item prefab must resolve to a GameObject. Assigned type: {summonItemPrefab.GetType().Name}");
                continue;
            }

            summonItemInstance.name = GetSummonItemName(gachaType, i);

            if (summonItemInstance.transform is RectTransform rectTransform)
                rectTransform.localScale = Vector3.one;

            if (!summonItemInstance.TryGetComponent(out CachaSummonItemUI summonItemUI))
            {
                Debug.LogError("[CachaUIController] Summon item prefab is missing CachaSummonItemUI.");
                continue;
            }

            summonItemUI.Bind(gachaType, GetSummonItemTitle(gachaType, i), crystalChangePopup, resultPopup, RefreshSummonItemUIs);
            summonItemUIs.Add(summonItemUI);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(summonItemContentRoot);
    }

    private void ClearSummonItems()
    {
        summonItemUIs.Clear();

        for (int i = summonItemContentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = summonItemContentRoot.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void RefreshSummonItemUIs()
    {
        for (int i = 0; i < summonItemUIs.Count; i++)
        {
            if (summonItemUIs[i] == null)
                continue;

            summonItemUIs[i].RefreshUI();
        }
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != CurrencyType.Crystal)
            return;

        RefreshSummonItemUIs();
    }

    private void OnItemAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        int ticketItemId = GachaTicketResolver.GetTicketItemId(currentSubMenu);
        if (ticketItemId <= 0 || item.ItemId != ticketItemId)
            return;

        RefreshSummonItemUIs();
    }

    private static GachaType SanitizeSubMenu(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Weapon:
            case GachaType.Armor:
            case GachaType.SkillScroll:
                return gachaType;
            default:
                return GachaType.Weapon;
        }
    }

    private static int GetSummonItemCount(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Weapon:
            case GachaType.Armor:
            case GachaType.SkillScroll:
                return 1;
            default:
                return 0;
        }
    }

    private static string GetSummonItemName(GachaType gachaType, int index)
    {
        switch (gachaType)
        {
            case GachaType.Weapon:
                return "(Panel)SummonItem_Weapon";
            case GachaType.Armor:
                return "(Panel)SummonItem_Armor";
            case GachaType.SkillScroll:
                return "(Panel)SummonItem_SkillScroll";
            default:
                return "(Panel)SummonItem";
        }
    }

    private static string GetSummonItemTitle(GachaType gachaType, int index)
    {
        switch (gachaType)
        {
            case GachaType.Weapon:
                return "\uBB34\uAE30 \uC18C\uD658";
            case GachaType.Armor:
                return "\uBC29\uC5B4\uAD6C \uC18C\uD658";
            case GachaType.SkillScroll:
                return "\uC2A4\uD0AC \uC2A4\uD06C\uB864 \uC18C\uD658";
            default:
                return "\uC18C\uD658";
        }
    }
}
