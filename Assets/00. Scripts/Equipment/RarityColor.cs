using UnityEditor.Build.Pipeline;
using UnityEngine;
using UnityEngine.SocialPlatforms;

public static class RarityColor
{
    public static Color NormalColor = new Color(122 / 255f, 122 / 255f, 122 / 255f, 1.0f);
    public static Color UnCommonColor = new Color(0 / 255f, 255 / 255f, 0 / 255f, 1.0f);
    public static Color RareColor = new Color(255 / 255f, 0 / 255f, 255 / 255f, 1.0f);
    public static Color LegendaryColor = new Color(255 / 255f, 255 / 255f, 0 / 255f, 1.0f);
    public static Color MythicColor = new Color(255 / 255f, 100 / 255f, 0 / 255f, 1.0f);

    public static Color ItemGradeColor(RarityType type)
    {
        switch (type)
        {
            case RarityType.nomal:
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
}
