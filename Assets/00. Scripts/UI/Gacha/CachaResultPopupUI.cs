using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
    [SerializeField] private Sprite ticketRepeatCurrencyIcon;
    [SerializeField] private Sprite crystalRepeatCurrencyIcon;

    private readonly List<CachaResultItemUI> resultItemUIs = new List<CachaResultItemUI>();
    private Coroutine revealRoutine;
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
        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

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
        PlayRevealAnimation();
    }

    public void Hide()
    {
        repeatAction = null;
        if (revealRoutine != null)
        {
            StopCoroutine(revealRoutine);
            revealRoutine = null;
        }

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
            itemUI.SetRareResultFlag(entries[i].IsRare);
            itemInstance.SetActive(false);
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
        if (manager == null)
        {
            buttonRepeat.interactable = false;

            if (textRepeatRequireCount != null)
                textRepeatRequireCount.text = "0";

            if (imageRepeatCurrencyIcon != null)
                imageRepeatCurrencyIcon.sprite = defaultRepeatCurrencyIcon;

            return;
        }

        bool canRepeat = manager.TryGetSpendPreview(
            gachaType,
            drawCount,
            out CurrencyType spendCurrencyType,
            out int spendAmount,
            out _,
            out _);
        CurrencyType displayCurrencyType = spendCurrencyType;
        int displayAmount = spendCurrencyType == CurrencyType.Crystal
            ? GetCrystalCost(drawCount, spendAmount)
            : GetTicketCost(drawCount);

        buttonRepeat.interactable = canRepeat;

        if (textRepeatRequireCount != null)
            textRepeatRequireCount.text = displayAmount.ToString();

        if (imageRepeatCurrencyIcon != null)
            imageRepeatCurrencyIcon.sprite = GetRepeatCurrencyIcon(gachaType, displayCurrencyType) ?? defaultRepeatCurrencyIcon;
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
            return crystalRepeatCurrencyIcon != null ? crystalRepeatCurrencyIcon : LoadCrystalIcon();

        return ticketRepeatCurrencyIcon != null ? ticketRepeatCurrencyIcon : LoadTicketIcon(gachaType);
    }

    private static CurrencyType GetTicketCurrency(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Armor:
                return CurrencyType.ArmorDrawTicket;
            case GachaType.SkillScroll:
                return CurrencyType.SkillScrollDrawTicket;
            case GachaType.Weapon:
            default:
                return CurrencyType.WeaponDrawTicket;
        }
    }

    private static int GetTicketCost(int drawCount)
    {
        return drawCount * GachaConfig.TicketCostPerDraw;
    }

    private static int GetCrystalCost(int drawCount, int previewSpendAmount)
    {
        if (previewSpendAmount > 0)
            return previewSpendAmount;

        int purchaseCount = ((drawCount - 1) / GachaConfig.TicketCostPerDraw + 1) * GachaConfig.TicketCostPerDraw;
        return purchaseCount * GachaConfig.CrystalCostPerDraw;
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
        Dictionary<int, int> itemIndexById = gachaType == GachaType.SkillScroll ? null : new Dictionary<int, int>();

        if (result.ItemIds == null)
            return entries;

        for (int i = 0; i < result.ItemIds.Count; i++)
        {
            int itemId = result.ItemIds[i];
            if (itemId <= 0)
                continue;
            bool isRare = result.ItemRareFlags != null
                && i < result.ItemRareFlags.Count
                && result.ItemRareFlags[i];
            int itemCount = result.ItemCounts != null
                && i < result.ItemCounts.Count
                && result.ItemCounts[i] > 0
                ? result.ItemCounts[i]
                : 1;

            if (gachaType == GachaType.SkillScroll)
            {
                // 스킬스크롤은 1회 뽑기 결과를 1슬롯으로 유지한다.
                entries.Add(new ResultEntry(itemId, itemCount, false));
                continue;
            }

            if (ShouldDisplayAsSingleEntry(itemId))
            {
                entries.Add(new ResultEntry(itemId, itemCount, isRare));
                continue;
            }

            if (itemIndexById != null && itemIndexById.TryGetValue(itemId, out int entryIndex))
            {
                ResultEntry entry = entries[entryIndex];
                entry.Count++;
                entry.IsRare = entry.IsRare || isRare;
                entries[entryIndex] = entry;
                continue;
            }

            if (itemIndexById != null)
                itemIndexById[itemId] = entries.Count;

            entries.Add(new ResultEntry(itemId, 1, isRare));
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
        public bool IsRare;

        public ResultEntry(int itemId, int count, bool isRare)
        {
            ItemId = itemId;
            Count = count;
            IsRare = isRare;
        }
    }

    private void PlayRevealAnimation()
    {
        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(CoRevealResultItems());
    }

    private IEnumerator CoRevealResultItems()
    {
        const float normalRevealIntervalSeconds = 0.1f;
        const float rareBeforeRevealSeconds = 0.5f;
        const float rareAfterRevealSeconds = 0.5f;

        for (int i = 0; i < resultItemUIs.Count; i++)
        {
            CachaResultItemUI itemUI = resultItemUIs[i];
            if (itemUI == null)
                continue;

            bool isRare = itemUI.IsRareResult;
            if (isRare)
                yield return new WaitForSecondsRealtime(rareBeforeRevealSeconds);

            itemUI.gameObject.SetActive(true);

            if (isRare)
            {
                itemUI.PlayRareEffect();
                yield return new WaitForSecondsRealtime(rareAfterRevealSeconds);
                continue;
            }

            yield return new WaitForSecondsRealtime(normalRevealIntervalSeconds);
        }

        revealRoutine = null;
    }
}
