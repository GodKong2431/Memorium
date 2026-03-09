using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipTierListView
{
    public readonly struct ItemRenderData
    {
        public readonly int ItemId;
        public readonly Sprite Icon;
        public readonly string LevelText;
        public readonly int StarCount;
        public readonly Color TierColor;
        public readonly Color FrameColor;
        public readonly bool IsDimmed;

        public ItemRenderData(
            int itemId,
            Sprite icon,
            string levelText,
            int starCount,
            Color tierColor,
            Color frameColor,
            bool isDimmed)
        {
            ItemId = itemId;
            Icon = icon;
            LevelText = levelText;
            StarCount = starCount;
            TierColor = tierColor;
            FrameColor = frameColor;
            IsDimmed = isDimmed;
        }
    }

    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

    private readonly RectTransform root;
    private readonly GameObject tierPrefab;
    private readonly GameObject itemPrefab;
    private readonly bool clearOnBuild;

    public EquipTierListView(RectTransform root, GameObject tierPrefab, GameObject itemPrefab, bool clearOnBuild)
    {
        this.root = root;
        this.tierPrefab = tierPrefab;
        this.itemPrefab = itemPrefab;
        this.clearOnBuild = clearOnBuild;
    }

    public Dictionary<int, EquipItemView> Build(
        IReadOnlyDictionary<int, List<ItemRenderData>> itemsByTier,
        GameObject starPrefab,
        Action<int> onClick)
    {
        // 티어 그룹과 장비 아이템을 1회 생성하고 View를 반환한다.
        Dictionary<int, EquipItemView> views = new Dictionary<int, EquipItemView>();

        if (clearOnBuild)
            ClearChildren();

        List<int> orderedTiers = new List<int>(itemsByTier.Keys);
        orderedTiers.Sort();

        for (int tierIndex = 0; tierIndex < orderedTiers.Count; tierIndex++)
        {
            int tier = orderedTiers[tierIndex];
            List<ItemRenderData> items = itemsByTier[tier];
            if (items == null || items.Count == 0)
                continue;

            ItemRenderData tierData = items[0];

            GameObject tierObject = UnityEngine.Object.Instantiate(tierPrefab, root, false);
            tierObject.name = $"Tier_{tier:00}";

            EquipTierUI tierUI = tierObject.GetComponent<EquipTierUI>();
            if (tierUI == null)
                continue;

            RenderTier(tierUI, starPrefab, tierData.StarCount, tierData.TierColor);

            for (int i = 0; i < items.Count; i++)
            {
                ItemRenderData itemData = items[i];

                GameObject itemObject = UnityEngine.Object.Instantiate(itemPrefab, tierUI.ListRoot, false);
                itemObject.name = $"Equipment_{itemData.ItemId}";

                EquipItemUI itemUI = itemObject.GetComponent<EquipItemUI>();
                if (itemUI == null)
                    continue;

                EquipItemView itemView = new EquipItemView(itemUI);

                int itemId = itemData.ItemId;
                itemView.Bind(() => onClick.Invoke(itemId));

                itemView.Render(
                    itemData.Icon,
                    itemData.LevelText,
                    itemData.StarCount,
                    itemData.TierColor);
                itemView.SetFrameColor(itemData.FrameColor);
                itemView.SetDimmed(itemData.IsDimmed);

                views[itemId] = itemView;
            }
        }

        return views;
    }

    private static void RenderTier(EquipTierUI tierUI, GameObject starPrefab, int starCount, Color tierColor)
    {
        // 티어 패널은 정렬용이므로 항상 투명으로 유지한다.
        tierUI.TierPanel.color = Transparent;

        int requiredStars = Mathf.Max(1, starCount);
        for (int i = 0; i < requiredStars; i++)
        {
            GameObject starObject = UnityEngine.Object.Instantiate(starPrefab, tierUI.TierRoot, false);
            starObject.name = $"(Img)TierStar_{i + 1}";

            Image starImage = starObject.GetComponent<Image>();
            if (starImage != null)
                starImage.color = tierColor;
        }
    }

    private void ClearChildren()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
    }
}
