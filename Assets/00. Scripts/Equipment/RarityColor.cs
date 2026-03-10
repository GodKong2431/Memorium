using UnityEngine;

public static class RarityColor
{
    // 장비 희귀도/티어 UI 색상 규칙
    public static readonly Color NormalColor = Color.white;
    public static readonly Color UnCommonColor = new Color(0.4f, 0.85f, 0.5f, 1f);
    public static readonly Color RareColor = new Color(0.72f, 0.45f, 1f, 1f);
    public static readonly Color LegendaryColor = new Color(1f, 0.85f, 0.2f, 1f);
    public static readonly Color MythicColor = new Color(1f, 0.55f, 0.2f, 1f);

    public static Color ItemGradeColor(RarityType type)
    {
        switch (type)
        {
            case RarityType.normal:
                return NormalColor;
            case RarityType.uncommon:
                return UnCommonColor;
            case RarityType.rare:
                return RareColor;
            case RarityType.legendary:
                return LegendaryColor;
            case RarityType.mythic:
                return MythicColor;
            default:
                return Color.white;
        }
    }

    public static Color TierColorByTier(int tier)
    {
        int safeTier = Mathf.Max(1, tier);
        int rarityIndex = Mathf.Clamp((safeTier - 1) / 5, 0, 4);
        return ItemGradeColor((RarityType)rarityIndex);
    }

    public static Color ColorByOrderIndex(int orderIndex)
    {
        int safeIndex = Mathf.Clamp(orderIndex, 0, 4);
        return ItemGradeColor((RarityType)safeIndex);
    }
}
