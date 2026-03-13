using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CachaResultItemUI : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private Button buttonItem;
    [SerializeField] private Image imageIcon;
    [SerializeField] private Image[] frameImages;
    [SerializeField] private RectTransform panelTierRoot;
    [SerializeField] private Image imageTierStarTemplate;
    [SerializeField] private RectTransform panelGemRoot;
    [SerializeField] private RectTransform panelItemCountOrLevelRoot;
    [SerializeField] private TextMeshProUGUI textCountOrLevel;

    private readonly List<Image> tierStars = new List<Image>();
    private Color[] defaultFrameColors;

    private void Awake()
    {
        if (imageTierStarTemplate == null && panelTierRoot != null && panelTierRoot.childCount > 0)
            imageTierStarTemplate = panelTierRoot.GetChild(0).GetComponent<Image>();

        tierStars.Clear();
        if (imageTierStarTemplate != null)
            tierStars.Add(imageTierStarTemplate);

        if (frameImages != null && frameImages.Length > 0)
        {
            defaultFrameColors = new Color[frameImages.Length];
            for (int i = 0; i < frameImages.Length; i++)
                defaultFrameColors[i] = frameImages[i] != null ? frameImages[i].color : Color.white;
        }

        if (buttonItem != null)
            buttonItem.onClick.RemoveAllListeners();
    }

    public void BindForResult(int itemId, int count)
    {
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad)
        {
            ApplyUnknownItem(count);
            return;
        }

        if (TryBindEquipment(itemId, count))
            return;

        if (TryBindStackItem(itemId, count))
            return;

        ApplyUnknownItem(count);
    }

    public void SetCountDisplay(int count)
    {
        if (panelItemCountOrLevelRoot != null)
            panelItemCountOrLevelRoot.gameObject.SetActive(true);

        if (textCountOrLevel != null)
            textCountOrLevel.text = Mathf.Max(0, count).ToString();
    }

    public void SetLevelDisplay(string levelText)
    {
        if (panelItemCountOrLevelRoot != null)
            panelItemCountOrLevelRoot.gameObject.SetActive(!string.IsNullOrWhiteSpace(levelText));

        if (textCountOrLevel != null)
            textCountOrLevel.text = levelText ?? string.Empty;
    }

    public void HideCountOrLevelDisplay()
    {
        if (panelItemCountOrLevelRoot != null)
            panelItemCountOrLevelRoot.gameObject.SetActive(false);

        if (textCountOrLevel != null)
            textCountOrLevel.text = string.Empty;
    }

    private bool TryBindEquipment(int itemId, int count)
    {
        if (DataManager.Instance.EquipListDict == null)
            return false;

        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable equipTable) || equipTable == null)
            return false;

        if (imageIcon != null)
            imageIcon.sprite = LoadSprite(string.IsNullOrEmpty(equipTable.iconResource) ? equipTable.equipmentName : equipTable.iconResource);

        HideCountOrLevelDisplay();
        SetGemVisible(false);
        SetTierVisible(true);
        SetTierStarCount(Mathf.Max(1, equipTable.grade), RarityColor.TierColorByTier(equipTable.grade));
        SetFrameColor(RarityColor.ItemGradeColor(equipTable.rarityType));
        return true;
    }

    private bool TryBindStackItem(int itemId, int count)
    {
        if (DataManager.Instance.ItemInfoDict == null)
            return false;

        if (!DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out ItemInfoTable itemInfo) || itemInfo == null)
            return false;

        if (imageIcon != null)
            imageIcon.sprite = LoadSprite(itemInfo.itemIcon);

        SetCountDisplay(count);
        SetTierVisible(false);
        SetGemVisible(itemInfo.itemType == ItemType.SkillScroll);
        ResetFrameColor();
        return true;
    }

    private void ApplyUnknownItem(int count)
    {
        if (imageIcon != null)
            imageIcon.sprite = null;

        SetCountDisplay(count);
        SetTierVisible(false);
        SetGemVisible(false);
        ResetFrameColor();
    }

    private void SetTierVisible(bool isVisible)
    {
        if (panelTierRoot != null)
            panelTierRoot.gameObject.SetActive(isVisible);
    }

    private void SetGemVisible(bool isVisible)
    {
        if (panelGemRoot != null)
            panelGemRoot.gameObject.SetActive(isVisible);
    }

    private void SetFrameColor(Color color)
    {
        if (frameImages == null)
            return;

        for (int i = 0; i < frameImages.Length; i++)
        {
            if (frameImages[i] == null)
                continue;

            frameImages[i].color = color;
        }
    }

    private void ResetFrameColor()
    {
        if (frameImages == null || defaultFrameColors == null)
            return;

        int colorCount = Mathf.Min(frameImages.Length, defaultFrameColors.Length);
        for (int i = 0; i < colorCount; i++)
        {
            if (frameImages[i] == null)
                continue;

            frameImages[i].color = defaultFrameColors[i];
        }
    }

    private void SetTierStarCount(int grade, Color color)
    {
        if (panelTierRoot == null || imageTierStarTemplate == null)
            return;

        int starCount = GetTierStarCount(grade);
        while (tierStars.Count < starCount)
        {
            Image clone = Instantiate(imageTierStarTemplate, panelTierRoot, false);
            clone.name = $"(Img)TierStar_{tierStars.Count + 1}";
            tierStars.Add(clone);
        }

        for (int i = 0; i < tierStars.Count; i++)
        {
            bool isActive = i < starCount;
            tierStars[i].gameObject.SetActive(isActive);
            if (isActive)
                tierStars[i].color = color;
        }
    }

    private static int GetTierStarCount(int grade)
    {
        return ((Mathf.Max(1, grade) - 1) % 5) + 1;
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
}
