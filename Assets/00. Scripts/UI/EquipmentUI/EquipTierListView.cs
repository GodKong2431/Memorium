using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EquipTierListView
{
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
        IReadOnlyDictionary<int, List<EquipListTable>> tableByTier,
        GameObject starPrefab,
        Func<int, Color> getTierColor,
        Func<int, Color> getOrderColor,
        Func<int, bool> isDimmed,
        Func<int, int> getStarCount,
        Func<int, string> getLevelText,
        Func<string, Sprite> getIcon,
        Action<int> onClick)
    {
        // 티어 그룹과 장비 아이템을 1회 생성하고 View를 반환한다.
        Dictionary<int, EquipItemView> views = new Dictionary<int, EquipItemView>();

        if (clearOnBuild)
            ClearChildren();

        List<int> orderedTiers = new List<int>(tableByTier.Keys);
        orderedTiers.Sort();

        for (int tierIndex = 0; tierIndex < orderedTiers.Count; tierIndex++)
        {
            int tier = orderedTiers[tierIndex];
            Color tierColor = getTierColor.Invoke(tier);
            int starCount = getStarCount.Invoke(tier);

            GameObject tierObject = UnityEngine.Object.Instantiate(tierPrefab, root, false);
            tierObject.name = $"Tier_{tier:00}";

            EquipTierUI tierUI = tierObject.GetComponent<EquipTierUI>();
            EquipTierView tierView = new EquipTierView(tierUI, starPrefab);
            tierView.Render(starCount, tierColor);

            List<EquipListTable> tables = tableByTier[tier];
            tables.Sort((lhs, rhs) => lhs.rarityType.CompareTo(rhs.rarityType));

            for (int i = 0; i < tables.Count; i++)
            {
                EquipListTable table = tables[i];

                GameObject itemObject = UnityEngine.Object.Instantiate(itemPrefab, tierUI.ListRoot, false);
                itemObject.name = $"Equipment_{table.ID}";

                EquipItemUI itemUI = itemObject.GetComponent<EquipItemUI>();
                EquipItemView itemView = new EquipItemView(itemUI);

                int itemId = table.ID;
                itemView.Bind(() => onClick.Invoke(itemId));

                string iconKey = string.IsNullOrEmpty(table.iconResource)
                    ? table.equipmentName
                    : table.iconResource;

                itemView.Render(
                    getIcon.Invoke(iconKey),
                    getLevelText.Invoke(table.ID),
                    starCount,
                    tierColor);
                itemView.SetFrameColor(getOrderColor.Invoke(i));
                itemView.SetDimmed(isDimmed.Invoke(itemId));

                views[itemId] = itemView;
            }
        }

        return views;
    }

    private void ClearChildren()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
    }
}

