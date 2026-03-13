using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CachaResultPopupUI : MonoBehaviour
{
    private static readonly Vector2 SinglePanelSize = new Vector2(900f, 577.17f);
    private static readonly Vector2 TenPanelSize = new Vector2(900f, 710.3f);
    private static readonly Vector2 ThirtyPanelSize = new Vector2(900f, 1300f);
    private static readonly Vector2 SingleScrollViewSize = new Vector2(850f, 300f);
    private static readonly Vector2 TenScrollViewSize = new Vector2(850f, 429.11f);
    private static readonly Vector2 ThirtyScrollViewSize = new Vector2(850f, 1030f);

    [Header("Layout")]
    [SerializeField] private RectTransform resultPanelRoot;
    [SerializeField] private RectTransform scrollViewRoot;
    [SerializeField] private ScrollRect scrollView;
    [SerializeField] private RectTransform contentRoot;
    [SerializeField] private GridLayoutGroup contentGridLayout;

    [Header("Item")]
    [SerializeField] private GameObject resultItemPrefab;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI textTitle;

    [Header("Button Group")]
    [SerializeField] private Button buttonClose;
    [SerializeField] private Button buttonRepeat;
    [SerializeField] private TextMeshProUGUI textRepeatAction;
    [SerializeField] private TextMeshProUGUI textRepeatCount;
    [SerializeField] private TextMeshProUGUI textRepeatRequireCount;
    [SerializeField] private Image imageRepeatCurrencyIcon;

    private readonly List<CachaResultItemUI> resultItemUIs = new List<CachaResultItemUI>();
    private Action repeatAction;
    private Sprite defaultRepeatCurrencyIcon;

    private void Awake()
    {
        if (imageRepeatCurrencyIcon != null)
            defaultRepeatCurrencyIcon = imageRepeatCurrencyIcon.sprite;

        if (buttonClose != null)
            buttonClose.onClick.AddListener(Hide);

        if (buttonRepeat != null)
            buttonRepeat.onClick.AddListener(OnRepeatClicked);

        if (textTitle != null)
            textTitle.text = "\uC18C\uD658 \uACB0\uACFC!";
    }

    private void OnDestroy()
    {
        if (buttonClose != null)
            buttonClose.onClick.RemoveListener(Hide);

        if (buttonRepeat != null)
            buttonRepeat.onClick.RemoveListener(OnRepeatClicked);
    }

    public void Show(GachaType gachaType, int drawCount, GachaDrawResult result, Action onRepeat)
    {
        repeatAction = onRepeat;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        RebuildResultItems(gachaType, result);
        RefreshRepeatButton(gachaType, drawCount);
        RefreshLayout(drawCount);
    }

    public void Hide()
    {
        repeatAction = null;

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void OnRepeatClicked()
    {
        repeatAction?.Invoke();
    }

    private void RebuildResultItems(GachaType gachaType, GachaDrawResult result)
    {
        ClearResultItems();

        if (contentRoot == null || resultItemPrefab == null)
            return;

        List<ResultEntry> entries = BuildResultItems(gachaType, result);
        for (int i = 0; i < entries.Count; i++)
        {
            GameObject itemInstance = Instantiate(resultItemPrefab, contentRoot, false);

            itemInstance.name = $"(Btn)Item_{i + 1}_{entries[i].ItemId}";
            if (!itemInstance.TryGetComponent(out CachaResultItemUI itemUI))
            {
                Debug.LogError("[CachaResultPopupUI] Result item prefab is missing CachaResultItemUI.");
                continue;
            }

            itemUI.BindForResult(entries[i].ItemId, entries[i].Count);
            resultItemUIs.Add(itemUI);
        }
    }

    private void ClearResultItems()
    {
        resultItemUIs.Clear();

        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = contentRoot.GetChild(i);
            child.SetParent(null, false);
            Destroy(child.gameObject);
        }
    }

    private void RefreshRepeatButton(GachaType gachaType, int drawCount)
    {
        if (textRepeatAction != null)
            textRepeatAction.text = "\uC18C\uD658";

        if (textRepeatCount != null)
            textRepeatCount.text = $"{drawCount}\uD68C";

        if (buttonRepeat == null)
            return;

        GachaManager manager = GachaManager.Instance;
        if (manager == null ||
            !manager.TryGetSpendPreview(gachaType, drawCount, out CurrencyType spendCurrencyType, out int spendAmount, out _, out _))
        {
            buttonRepeat.interactable = false;

            if (textRepeatRequireCount != null)
                textRepeatRequireCount.text = "0";

            if (imageRepeatCurrencyIcon != null)
                imageRepeatCurrencyIcon.sprite = defaultRepeatCurrencyIcon;

            return;
        }

            buttonRepeat.interactable = true;

        if (textRepeatRequireCount != null)
            textRepeatRequireCount.text = spendAmount.ToString();

        if (imageRepeatCurrencyIcon != null)
            imageRepeatCurrencyIcon.sprite = GetRepeatCurrencyIcon(gachaType, spendCurrencyType) ?? defaultRepeatCurrencyIcon;
    }

    private void RefreshLayout(int drawCount)
    {
        ApplyLayoutPreset(drawCount);
        RefreshContentLayout();

        if (scrollView != null)
        {
            scrollView.StopMovement();
            scrollView.verticalNormalizedPosition = 1f;
            scrollView.horizontalNormalizedPosition = 0f;
        }
    }

    private void ApplyLayoutPreset(int drawCount)
    {
        Vector2 panelSize;
        Vector2 scrollSize;
        GetLayoutPreset(drawCount, out panelSize, out scrollSize);

        if (resultPanelRoot != null)
            resultPanelRoot.sizeDelta = panelSize;

        if (scrollViewRoot != null)
            scrollViewRoot.sizeDelta = scrollSize;
    }

    private void RefreshContentLayout()
    {
        if (contentRoot != null && contentGridLayout != null)
        {
            int itemCount = Mathf.Max(1, resultItemUIs.Count);
            int columnCount = Mathf.Max(1, GetColumnCount());
            int rowCount = Mathf.Max(1, Mathf.CeilToInt((float)itemCount / columnCount));

            float contentHeight = contentGridLayout.padding.top
                + contentGridLayout.padding.bottom
                + (contentGridLayout.cellSize.y * rowCount)
                + (contentGridLayout.spacing.y * Mathf.Max(0, rowCount - 1));

            Vector2 contentSize = contentRoot.sizeDelta;
            contentSize.y = contentHeight;
            contentRoot.sizeDelta = contentSize;
        }

        Canvas.ForceUpdateCanvases();

        if (contentRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRoot);

        if (scrollViewRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(scrollViewRoot);

        if (resultPanelRoot != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(resultPanelRoot);
    }

    private int GetColumnCount()
    {
        if (contentGridLayout == null)
            return 1;

        if (contentGridLayout.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            return Mathf.Max(1, contentGridLayout.constraintCount);

        return 5;
    }

    private Sprite GetRepeatCurrencyIcon(GachaType gachaType, CurrencyType spendCurrencyType)
    {
        if (spendCurrencyType == CurrencyType.Crystal)
            return LoadCrystalIcon();

        return LoadTicketIcon(gachaType);
    }

    private Sprite LoadTicketIcon(GachaType gachaType)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.GachaTicketDict == null)
            return null;

        TicketType ticketType = ConvertToTicketType(gachaType);
        foreach (KeyValuePair<int, GachaTicketTable> pair in DataManager.Instance.GachaTicketDict)
        {
            GachaTicketTable table = pair.Value;
            if (table == null || table.ticketType != ticketType)
                continue;

            return LoadSprite(table.ticketResources);
        }

        return null;
    }

    private Sprite LoadCrystalIcon()
    {
        if (GachaManager.Instance == null || DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.ItemInfoDict == null)
            return null;

        int crystalId = GachaManager.Instance.crystalId;
        if (crystalId <= 0)
            return null;

        if (!DataManager.Instance.ItemInfoDict.TryGetValue(crystalId, out ItemInfoTable crystalInfo) || crystalInfo == null)
            return null;

        return LoadSprite(crystalInfo.itemIcon);
    }

    private static TicketType ConvertToTicketType(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Armor:
                return TicketType.Armor;
            case GachaType.SkillScroll:
                return TicketType.SkillScroll;
            case GachaType.Weapon:
            default:
                return TicketType.Weapon;
        }
    }

    private static void GetLayoutPreset(int drawCount, out Vector2 panelSize, out Vector2 scrollSize)
    {
        switch (drawCount)
        {
            case 1:
                panelSize = SinglePanelSize;
                scrollSize = SingleScrollViewSize;
                return;
            case 30:
                panelSize = ThirtyPanelSize;
                scrollSize = ThirtyScrollViewSize;
                return;
            case 10:
            default:
                panelSize = TenPanelSize;
                scrollSize = TenScrollViewSize;
                return;
        }
    }

    private static List<ResultEntry> BuildResultItems(GachaType gachaType, GachaDrawResult result)
    {
        List<ResultEntry> entries = new List<ResultEntry>();
        Dictionary<int, int> itemIndexById = gachaType == GachaType.SkillScroll ? new Dictionary<int, int>() : null;

        if (result.ItemIds == null)
            return entries;

        for (int i = 0; i < result.ItemIds.Count; i++)
        {
            int itemId = result.ItemIds[i];
            if (itemId <= 0)
                continue;

            if (ShouldDisplayAsSingleEntry(itemId))
            {
                entries.Add(new ResultEntry(itemId, 1));
                continue;
            }

            if (itemIndexById != null && itemIndexById.TryGetValue(itemId, out int entryIndex))
            {
                ResultEntry entry = entries[entryIndex];
                entry.Count++;
                entries[entryIndex] = entry;
                continue;
            }

            if (itemIndexById != null)
                itemIndexById[itemId] = entries.Count;

            entries.Add(new ResultEntry(itemId, 1));
        }

        return entries;
    }

    private static bool ShouldDisplayAsSingleEntry(int itemId)
    {
        return DataManager.Instance != null
            && DataManager.Instance.DataLoad
            && DataManager.Instance.EquipListDict != null
            && DataManager.Instance.EquipListDict.ContainsKey(itemId);
    }

    private static Sprite LoadSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string trimmedKey = key.Trim();
        Sprite sprite = Resources.Load<Sprite>(trimmedKey);
        if (sprite != null)
            return sprite;

        int extensionIndex = trimmedKey.LastIndexOf(".", StringComparison.Ordinal);
        if (extensionIndex > 0)
        {
            sprite = Resources.Load<Sprite>(trimmedKey.Substring(0, extensionIndex));
            if (sprite != null)
                return sprite;
        }

        const string resourcesToken = "Resources/";
        int resourcesIndex = trimmedKey.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0)
            return null;

        string relativePath = trimmedKey.Substring(resourcesIndex + resourcesToken.Length);
        int relativeExtensionIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
        if (relativeExtensionIndex > 0)
            relativePath = relativePath.Substring(0, relativeExtensionIndex);

        return Resources.Load<Sprite>(relativePath);
    }

    private struct ResultEntry
    {
        public int ItemId;
        public int Count;

        public ResultEntry(int itemId, int count)
        {
            ItemId = itemId;
            Count = count;
        }
    }
}
