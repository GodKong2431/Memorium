using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 준비된 렌더 데이터로 티어 그룹과 아이템 셀을 생성한다.
public sealed class EquipTierListView
{
    // 장비 아이템 셀 1개의 불변 렌더 데이터다.
    public readonly struct ItemRenderData
    {
        // 클릭/조회에 사용하는 아이템 ID다.
        public readonly int ItemId;
        // 표시할 아이콘 스프라이트다.
        public readonly Sprite Icon;
        // 표시할 레벨 텍스트다.
        public readonly string LevelText;
        // 표시할 별 개수다.
        public readonly int StarCount;
        // 티어에 적용할 색상이다.
        public readonly Color TierColor;
        // 프레임에 적용할 색상이다.
        public readonly Color FrameColor;
        // 초기 잠금(디밍) 상태다.
        public readonly bool IsDimmed;

        // 장비 셀 1개에 대한 렌더 데이터를 생성한다.
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

    // 티어 패널에 사용하는 투명 색상이다.
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

    // 티어 그룹을 생성할 루트 트랜스폼이다.
    private readonly RectTransform root;
    // 티어 그룹 프리팹이다.
    private readonly GameObject tierPrefab;
    // 아이템 셀 프리팹이다.
    private readonly GameObject itemPrefab;
    // 빌드 전 기존 자식 삭제 여부다.
    private readonly bool clearOnBuild;

    // 루트/프리팹 바인딩으로 티어 리스트 빌더를 초기화한다.
    public EquipTierListView(RectTransform root, GameObject tierPrefab, GameObject itemPrefab, bool clearOnBuild)
    {
        this.root = root;
        this.tierPrefab = tierPrefab;
        this.itemPrefab = itemPrefab;
        this.clearOnBuild = clearOnBuild;
    }

    // 티어별 렌더 데이터로 티어/아이템 뷰를 생성한다.
    public Dictionary<int, EquipItemView> Build(
        IReadOnlyDictionary<int, List<ItemRenderData>> itemsByTier,
        GameObject starPrefab,
        Action<int> onClick)
    {
        // 아이템 ID 기준으로 생성된 뷰를 보관한다.
        Dictionary<int, EquipItemView> views = new Dictionary<int, EquipItemView>();

        if (clearOnBuild)
            ClearChildren();

        // 티어 키를 정렬해 UI 순서를 고정한다.
        List<int> orderedTiers = new List<int>(itemsByTier.Keys);
        orderedTiers.Sort();

        for (int tierIndex = 0; tierIndex < orderedTiers.Count; tierIndex++)
        {
            // 현재 티어 그룹의 렌더 데이터 목록을 가져온다.
            int tier = orderedTiers[tierIndex];
            List<ItemRenderData> items = itemsByTier[tier];
            if (items == null || items.Count == 0)
                continue;

            // 첫 번째 데이터를 티어 헤더 렌더 시드로 사용한다.
            ItemRenderData tierData = items[0];

            // 티어 그룹 오브젝트를 생성한다.
            GameObject tierObject = UnityEngine.Object.Instantiate(tierPrefab, root, false);
            tierObject.name = $"Tier_{tier:00}";

            // 필수 티어 UI 바인딩을 찾는다.
            EquipTierUI tierUI = tierObject.GetComponent<EquipTierUI>();
            if (tierUI == null)
                continue;

            // 티어 헤더 UI를 렌더링한다.
            RenderTier(tierUI, starPrefab, tierData.StarCount, tierData.TierColor);

            for (int i = 0; i < items.Count; i++)
            {
                // 아이템 1개의 렌더 데이터를 가져온다.
                ItemRenderData itemData = items[i];

                // 아이템 셀 오브젝트를 생성한다.
                GameObject itemObject = UnityEngine.Object.Instantiate(itemPrefab, tierUI.ListRoot, false);
                itemObject.name = $"Equipment_{itemData.ItemId}";

                // 필수 아이템 UI 바인딩을 찾는다.
                EquipItemUI itemUI = itemObject.GetComponent<EquipItemUI>();
                if (itemUI == null)
                    continue;

                // 아이템 뷰를 만들고 클릭 이벤트를 연결한다.
                EquipItemView itemView = new EquipItemView(itemUI);
                int itemId = itemData.ItemId;
                itemView.Bind(() => onClick.Invoke(itemId));

                // 렌더 데이터를 아이템 셀에 반영한다.
                itemView.Render(
                    itemData.Icon,
                    itemData.LevelText,
                    itemData.StarCount,
                    itemData.TierColor);
                itemView.SetFrameColor(itemData.FrameColor);
                itemView.SetDimmed(itemData.IsDimmed);

                // 생성한 아이템 뷰를 ID 기준으로 저장한다.
                views[itemId] = itemView;
            }
        }

        return views;
    }

    // 티어 헤더 패널과 티어 별 아이콘을 렌더링한다.
    private static void RenderTier(EquipTierUI tierUI, GameObject starPrefab, int starCount, Color tierColor)
    {
        // 티어 패널은 레이아웃 용도로만 사용하므로 투명 처리한다.
        tierUI.TierPanel.color = Transparent;

        // 필요한 개수만큼 별 아이콘을 생성하고 색상을 적용한다.
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

    // 재빌드 전에 루트 하위 자식 오브젝트를 전부 삭제한다.
    private void ClearChildren()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(root.GetChild(i).gameObject);
    }
}
