using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class ItemDetailPopupUI : MonoBehaviour
{
    [Header("Popup")]
    [SerializeField] private RectTransform popupRoot;
    [SerializeField] private RectTransform popupOverlayParent;
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemTypeText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private RectTransform actionButtonRoot;
    [SerializeField] private bool closeOnStart = true;

    private PopupStackService.Handle popupHandle;

    private void Awake()
    {
        if (itemIconImage != null)
            itemIconImage.preserveAspect = true;

        if (actionButtonRoot != null)
            actionButtonRoot.gameObject.SetActive(false);

        if (closeOnStart)
            SetVisible(false);
    }

    private void OnDisable()
    {
        PopupStackService.Dismiss(ref popupHandle);
    }

    public void Show(int itemId)
    {
        if (itemId <= 0 || DataManager.Instance?.ItemInfoDict == null)
            return;

        if (!DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out ItemInfoTable itemInfo) || itemInfo == null)
            return;

        Show(itemInfo);
    }

    public void Show(ItemInfoTable itemInfo)
    {
        if (popupRoot == null || popupOverlayParent == null || itemInfo == null)
            return;

        Bind(itemInfo);

        if (actionButtonRoot != null)
            actionButtonRoot.gameObject.SetActive(false);

        SetVisible(true);
        PopupStackService.Present(ref popupHandle, new PopupStackService.Request
        {
            PopupRoot = popupRoot,
            ContentRoot = popupRoot,
            OverlayParent = popupOverlayParent,
            OnRequestClose = Hide,
            CloseOnOutside = true
        });
    }

    public void Hide()
    {
        PopupStackService.Dismiss(ref popupHandle);
        SetVisible(false);
    }

    private void Bind(ItemInfoTable itemInfo)
    {
        if (itemIconImage != null)
            itemIconImage.sprite = IconManager.GetItemIcon(itemInfo);

        if (itemNameText != null)
            itemNameText.text = ResolveLocalizedText(itemInfo.itemName);

        if (itemTypeText != null)
            itemTypeText.text = ResolveItemTypeText(itemInfo);

        if (itemDescriptionText != null)
            itemDescriptionText.text = ResolveItemDescription(itemInfo);
    }

    private void SetVisible(bool visible)
    {
        if (popupRoot != null && popupRoot.gameObject.activeSelf != visible)
            popupRoot.gameObject.SetActive(visible);
    }

    private static string ResolveItemTypeText(ItemInfoTable itemInfo)
    {
        if (itemInfo == null)
            return string.Empty;

        if (!string.IsNullOrWhiteSpace(itemInfo.itemTypedesc))
            return itemInfo.itemTypedesc.Trim();

        switch (itemInfo.itemType)
        {
            case ItemType.Weapon:
                return "\uBB34\uAE30";
            case ItemType.Helmet:
                return "\uD5EC\uBA67";
            case ItemType.Glove:
                return "\uC7A5\uAC11";
            case ItemType.Armor:
                return "\uAC11\uC637";
            case ItemType.Boots:
                return "\uBD80\uCE20";
            case ItemType.SkillScroll:
                return "\uC2A4\uD0AC \uC8FC\uBB38\uC11C";
            case ItemType.ElementGem:
                return "\uC6D0\uC18C \uBCF4\uC11D";
            case ItemType.UniqueGem:
                return "\uACE0\uC720 \uBCF4\uC11D";
            case ItemType.PixiePiece:
                return "\uC694\uC815 \uC870\uAC01";
            case ItemType.Pixie:
                return "\uC694\uC815";
            case ItemType.GachaTicket:
                return "\uC18C\uD658\uAD8C";
            case ItemType.BingoLink:
                return "\uBE59\uACE0 \uB9C1\uD06C";
            case ItemType.BingoItem_A:
            case ItemType.BingoItem_B:
                return "\uBE59\uACE0 \uC544\uC774\uD15C";
            case ItemType.BingoSynergy:
                return "\uBE59\uACE0 \uC2DC\uB108\uC9C0";
            case ItemType.FreeCurrency:
                return "\uACE8\uB4DC";
            case ItemType.PaidCurrency:
                return "\uD06C\uB9AC\uC2A4\uD0C8";
            case ItemType.Key:
                return "\uD0A4";
            default:
                return itemInfo.itemType.ToString();
        }
    }

    private static string ResolveItemDescription(ItemInfoTable itemInfo)
    {
        if (itemInfo == null)
            return string.Empty;

        string description = ResolveLocalizedText(itemInfo.itemDesc);
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        description = ResolveLocalizedText(itemInfo.desc);
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        return string.Empty;
    }

    private static string ResolveLocalizedText(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        string trimmedValue = rawValue.Trim();
        if (DataManager.Instance?.StringDict != null)
        {
            foreach (StringTable entry in DataManager.Instance.StringDict.Values)
            {
                if (entry == null ||
                    string.IsNullOrWhiteSpace(entry.stringKey) ||
                    !string.Equals(entry.stringKey, trimmedValue, StringComparison.Ordinal))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(entry.Kr))
                    return entry.Kr.Trim();

                break;
            }
        }

        return trimmedValue;
    }
}
