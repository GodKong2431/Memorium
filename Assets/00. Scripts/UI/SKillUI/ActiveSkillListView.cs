using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ActiveSkill 아이템을 1회 생성하고 View 캐시를 반환한다.
/// </summary>
public sealed class ActiveSkillItemListBuilder
{
    private readonly RectTransform listRoot;
    private readonly GameObject itemPrefab;
    private readonly bool clearOnBuild;

    public ActiveSkillItemListBuilder(RectTransform listRoot, GameObject itemPrefab, bool clearOnBuild)
    {
        this.listRoot = listRoot;
        this.itemPrefab = itemPrefab;
        this.clearOnBuild = clearOnBuild;
    }

    public Dictionary<int, ActiveSkillItemView> BuildItems(
        IReadOnlyList<int> skillIds,
        Action<int> onClickAction,
        float lockedHeight,
        float upgradeHeight)
    {
        Dictionary<int, ActiveSkillItemView> itemViews = new Dictionary<int, ActiveSkillItemView>();

        if (listRoot == null || itemPrefab == null)
            return itemViews;

        if (clearOnBuild)
            ClearList();

        for (int i = 0; i < skillIds.Count; i++)
        {
            int skillId = skillIds[i];
            GameObject itemObject = UnityEngine.Object.Instantiate(itemPrefab, listRoot, false);
            itemObject.name = $"ActiveSkillItem_{skillId}";

            ActiveSkillItemBinding binding = itemObject.GetComponent<ActiveSkillItemBinding>();
            if (binding == null)
            {
                Debug.LogWarning($"[ActiveSkillItemListBuilder] ActiveSkillItemBinding missing: {itemObject.name}");
                continue;
            }

            ActiveSkillItemView itemView = new ActiveSkillItemView(binding, lockedHeight, upgradeHeight);
            int capturedSkillId = skillId;
            itemView.SetMergeClickHandler(() => onClickAction?.Invoke(capturedSkillId));
            itemViews[capturedSkillId] = itemView;
        }

        return itemViews;
    }

    private void ClearList()
    {
        for (int i = listRoot.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(listRoot.GetChild(i).gameObject);
    }
}
