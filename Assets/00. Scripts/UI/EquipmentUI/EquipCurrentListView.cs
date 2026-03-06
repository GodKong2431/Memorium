using System.Collections.Generic;
using UnityEngine;

public sealed class EquipCurrentListView
{
    private readonly RectTransform root;
    private readonly GameObject itemPrefab;

    public EquipCurrentListView(RectTransform root, GameObject itemPrefab)
    {
        this.root = root;
        this.itemPrefab = itemPrefab;
    }

    public List<EquipItemView> Build(IReadOnlyList<EquipmentType> order)
    {
        // 현재 장착 UI는 항상 고정 순서로 새로 구성한다.
        List<EquipItemView> views = new List<EquipItemView>();

        for (int i = root.childCount - 1; i >= 0; i--)
            Object.Destroy(root.GetChild(i).gameObject);

        for (int i = 0; i < order.Count; i++)
        {
            GameObject go = Object.Instantiate(itemPrefab, root, false);
            go.name = $"CurrentEquipment_{order[i]}";

            EquipItemUI ui = go.GetComponent<EquipItemUI>();
            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = false;
            ui.MergeSlider.gameObject.SetActive(false);

            views.Add(new EquipItemView(ui));
        }

        return views;
    }
}

