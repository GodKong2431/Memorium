using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipTierView
{
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);

    private readonly EquipTierUI ui;
    private readonly GameObject starPrefab;
    private readonly List<Image> stars = new List<Image>();

    public EquipTierView(EquipTierUI ui, GameObject starPrefab)
    {
        this.ui = ui;
        this.starPrefab = starPrefab;
    }

    public void Render(int starCount, Color tierColor)
    {
        // 티어 패널은 정렬용이므로 항상 투명으로 유지한다.
        ui.TierPanel.color = Transparent;

        SyncStars(Mathf.Max(1, starCount));
        for (int i = 0; i < stars.Count; i++)
        {
            if (!stars[i].gameObject.activeSelf)
                continue;

            stars[i].color = tierColor;
        }
    }

    private void SyncStars(int required)
    {
        while (stars.Count < required)
        {
            GameObject starObject = Object.Instantiate(starPrefab, ui.TierRoot, false);
            starObject.name = $"(Img)TierStar_{stars.Count + 1}";
            stars.Add(starObject.GetComponent<Image>());
        }

        for (int i = 0; i < stars.Count; i++)
            stars[i].gameObject.SetActive(i < required);
    }
}

