using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스탯 업그레이드 아이템 오브젝트를 생성하고 타입별 뷰를 반환한다.
/// </summary>
public sealed class PlayerStatUpgradeListView
{
    private readonly RectTransform contentRoot;
    private readonly GameObject statItemPrefab;
    private readonly bool clearExistingChildrenOnBuild;

    public PlayerStatUpgradeListView(
        RectTransform contentRoot,
        GameObject statItemPrefab,
        bool clearExistingChildrenOnBuild)
    {
        this.contentRoot = contentRoot;
        this.statItemPrefab = statItemPrefab;
        this.clearExistingChildrenOnBuild = clearExistingChildrenOnBuild;
    }

    public Dictionary<StatType, PlayerStatUpgradeItemView> Build(
        IReadOnlyList<StatType> statTypes,
        Action<StatType> onClickUpgrade)
    {
        // 모든 행을 1회 생성하고, StatType 기준 캐시 딕셔너리를 만든다.
        Dictionary<StatType, PlayerStatUpgradeItemView> itemViews = new Dictionary<StatType, PlayerStatUpgradeItemView>();

        if (clearExistingChildrenOnBuild)
            ClearChildren();

        for (int i = 0; i < statTypes.Count; i++)
        {
            StatType statType = statTypes[i];
            UnityEngine.Object clonedObject = UnityEngine.Object.Instantiate((UnityEngine.Object)statItemPrefab, contentRoot, false);
            GameObject itemObject = clonedObject switch
            {
                GameObject go => go,
                Component component => component.gameObject,
                _ => throw new InvalidOperationException("StatItem 프리팹은 GameObject 또는 Component여야 합니다.")
            };
            itemObject.name = $"StatItem_{statType}";

            PlayerStatUpgradeItem item = itemObject.GetComponent<PlayerStatUpgradeItem>();
            PlayerStatUpgradeItemView itemView = new PlayerStatUpgradeItemView(item);
            StatType cachedStatType = statType;
            itemView.BindUpgradeButton(() => onClickUpgrade.Invoke(cachedStatType));
            itemViews[cachedStatType] = itemView;
        }

        return itemViews;
    }

    private void ClearChildren()
    {
        // 초기 빌드 시 콘텐츠 루트를 비우고 새로 구성할 때 사용한다.
        for (int i = contentRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(contentRoot.GetChild(i).gameObject);
    }
}
