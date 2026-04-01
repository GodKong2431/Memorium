using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class CachaCrystalChangePopupUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textInfo;
    [SerializeField] private Button buttonConfirm;
    [SerializeField] private TextMeshProUGUI textConfirmAction;
    [SerializeField] private TextMeshProUGUI textConfirmCount;
    [SerializeField] private TextMeshProUGUI textRequireCount;
    [SerializeField] private Image imageCurrencyIcon;
    [SerializeField] private Button buttonCancel;

    private Action confirmAction;
    private Sprite crystalIconSprite;
    private PopupStackService.Handle popupHandle;

    private void Awake()
    {
        if (imageCurrencyIcon != null)
            crystalIconSprite = imageCurrencyIcon.sprite;

        if (buttonConfirm != null)
            buttonConfirm.onClick.AddListener(OnConfirmClicked);

        if (buttonCancel != null)
            buttonCancel.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        PopupStackService.Dismiss(ref popupHandle);

        if (buttonConfirm != null)
            buttonConfirm.onClick.RemoveListener(OnConfirmClicked);

        if (buttonCancel != null)
            buttonCancel.onClick.RemoveListener(Hide);
    }

    public bool Show(GachaType gachaType, string summonTitle, int drawCount, Action onConfirm)
    {
        GachaManager manager = GachaManager.Instance;
        if (manager == null)
            return false;

        if (!manager.TryGetSpendPreview(
            gachaType,
            drawCount,
            out CurrencyType spendCurrencyType,
            out int spendAmount,
            out int ownedTicketCount,
            out int missingTicketCount))
        {
            return false;
        }

        if (spendCurrencyType != CurrencyType.Crystal)
            return false;

        confirmAction = onConfirm;
        ApplyPresentation(gachaType, summonTitle, drawCount, spendAmount, ownedTicketCount, missingTicketCount);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        RectTransform popupRoot = transform as RectTransform;
        PopupStackService.Present(ref popupHandle, new PopupStackService.Request
        {
            PopupRoot = popupRoot,
            ContentRoot = popupRoot,
            OverlayParent = popupRoot != null ? popupRoot.parent as RectTransform : null,
            OnRequestClose = Hide,
            CloseOnOutside = false
        });

        return true;
    }

    public void Hide()
    {
        confirmAction = null;
        PopupStackService.Dismiss(ref popupHandle);

        if (gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        Action action = confirmAction;
        Hide();
        action?.Invoke();
    }

    private void ApplyPresentation(
        GachaType gachaType,
        string summonTitle,
        int drawCount,
        int spendAmount,
        int ownedTicketCount,
        int missingTicketCount)
    {
        if (textInfo != null)
            textInfo.text = BuildInfoText(gachaType, summonTitle, drawCount, spendAmount, ownedTicketCount, missingTicketCount);

        if (textConfirmAction != null)
            textConfirmAction.text = "\uC18C\uD658";

        if (textConfirmCount != null)
            textConfirmCount.text = $"{drawCount}\uD68C";

        if (textRequireCount != null)
            textRequireCount.text = spendAmount.ToString();

        if (imageCurrencyIcon != null)
        {
            imageCurrencyIcon.gameObject.SetActive(true);
            if (crystalIconSprite != null)
                imageCurrencyIcon.sprite = crystalIconSprite;
        }

        if (buttonConfirm != null)
            buttonConfirm.interactable = true;

        if (buttonCancel != null)
            buttonCancel.interactable = true;
    }

    private static string BuildInfoText(
        GachaType gachaType,
        string summonTitle,
        int drawCount,
        int spendAmount,
        int ownedTicketCount,
        int missingTicketCount)
    {
        string ticketName = GetTicketDisplayName(gachaType);
        int shortage = Mathf.Max(0, missingTicketCount);
        return $"{ticketName} {shortage}\uAC1C \uB300\uC2E0 \uD06C\uB9AC\uC2A4\uD0C8 {spendAmount}\uAC1C\uB97C \uC0AC\uC6A9 \uD558\uC2DC\uACA0\uC2B5\uB2C8\uAE4C?";
    }

    private static string GetDefaultSummonTitle(GachaType gachaType)
    {
        switch (gachaType)
        {
            case GachaType.Weapon:
                return "\uBB34\uAE30 \uC18C\uD658";
            case GachaType.Armor:
                return "\uBC29\uC5B4\uAD6C \uC18C\uD658";
            case GachaType.SkillScroll:
                return "\uC2A4\uD0AC \uC2A4\uD06C\uB864 \uC18C\uD658";
            default:
                return "\uC18C\uD658";
        }
    }

    private static string GetTicketDisplayName(GachaType gachaType)
    {
        if (GachaTicketResolver.TryGetTicketTable(gachaType, out GachaTicketTable table) &&
            table != null &&
            !string.IsNullOrWhiteSpace(table.ticketName))
        {
            return table.ticketName;
        }

        switch (gachaType)
        {
            case GachaType.Weapon:
                return "\uBB34\uAE30 \uC18C\uD658\uAD8C";
            case GachaType.Armor:
                return "\uBC29\uC5B4\uAD6C \uC18C\uD658\uAD8C";
            case GachaType.SkillScroll:
                return "\uC2A4\uD0AC \uC2A4\uD06C\uB864 \uC18C\uD658\uAD8C";
            default:
                return "\uC18C\uD658\uAD8C";
        }
    }

}
