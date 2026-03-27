using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class OptionGamePanelUI : MonoBehaviour, IPointerClickHandler
{
    private const string PixieCheckboxName = "checkbox_PixieEffact";
    private const string GachaCheckboxName = "checkbox_gacha";
    private const string BerserkerCheckboxName = "checkbox_Berserker";

    private Image pixieCheckbox;
    private Image gachaCheckbox;
    private Image berserkerCheckbox;
    private Sprite offSprite;
    private Sprite onSprite;

    private void Awake()
    {
        CacheReferences();
        CacheSprites();
        RefreshVisuals();
    }

    private void OnEnable()
    {
        RefreshVisuals();

        GameOptionSettings.HidePixieDebuffEffectChanged += HandleHidePixieDebuffEffectChanged;
        GameOptionSettings.SkipGachaCrystalPopupChanged += HandleSkipGachaCrystalPopupChanged;
        GameOptionSettings.UseManualBerserkerModeChanged += HandleUseManualBerserkerModeChanged;
    }

    private void OnDisable()
    {
        GameOptionSettings.HidePixieDebuffEffectChanged -= HandleHidePixieDebuffEffectChanged;
        GameOptionSettings.SkipGachaCrystalPopupChanged -= HandleSkipGachaCrystalPopupChanged;
        GameOptionSettings.UseManualBerserkerModeChanged -= HandleUseManualBerserkerModeChanged;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null)
            return;

        Transform clickedTransform = GetClickedTransform(eventData);
        if (clickedTransform == null)
            return;

        Image clickedCheckbox = FindCheckboxImage(clickedTransform);
        if (clickedCheckbox == null)
            return;

        if (clickedCheckbox == pixieCheckbox)
            GameOptionSettings.HidePixieDebuffEffect = !GameOptionSettings.HidePixieDebuffEffect;
        else if (clickedCheckbox == gachaCheckbox)
            GameOptionSettings.SkipGachaCrystalPopup = !GameOptionSettings.SkipGachaCrystalPopup;
        else if (clickedCheckbox == berserkerCheckbox)
            GameOptionSettings.UseManualBerserkerMode = !GameOptionSettings.UseManualBerserkerMode;
    }

    private void CacheReferences()
    {
        pixieCheckbox = FindCheckbox(PixieCheckboxName);
        gachaCheckbox = FindCheckbox(GachaCheckboxName);
        berserkerCheckbox = FindCheckbox(BerserkerCheckboxName);
    }

    private void CacheSprites()
    {
        if (offSprite == null)
            offSprite = SelectSprite(pixieCheckbox, gachaCheckbox, berserkerCheckbox);

        if (onSprite == null)
            onSprite = SelectSprite(berserkerCheckbox, pixieCheckbox, gachaCheckbox);
    }

    private void RefreshVisuals()
    {
        CacheReferences();
        CacheSprites();

        ApplyCheckboxState(pixieCheckbox, GameOptionSettings.HidePixieDebuffEffect);
        ApplyCheckboxState(gachaCheckbox, GameOptionSettings.SkipGachaCrystalPopup);
        ApplyCheckboxState(berserkerCheckbox, GameOptionSettings.UseManualBerserkerMode);
    }

    private void ApplyCheckboxState(Image checkbox, bool isOn)
    {
        if (checkbox == null)
            return;

        bool canSwapSprite = onSprite != null && offSprite != null && onSprite != offSprite;
        if (canSwapSprite)
            checkbox.sprite = isOn ? onSprite : offSprite;

        checkbox.color = canSwapSprite || isOn
            ? Color.white
            : new Color(1f, 1f, 1f, 0.7f);
    }

    private Image FindCheckbox(string checkboxName)
    {
        Transform checkboxTransform = transform.Find(checkboxName);
        return checkboxTransform != null ? checkboxTransform.GetComponent<Image>() : null;
    }

    private Image FindCheckboxImage(Transform clickedTransform)
    {
        Transform current = clickedTransform;
        while (current != null && current != transform)
        {
            if (current.name == PixieCheckboxName ||
                current.name == GachaCheckboxName ||
                current.name == BerserkerCheckboxName)
            {
                return current.GetComponent<Image>();
            }

            current = current.parent;
        }

        return null;
    }

    private static Transform GetClickedTransform(PointerEventData eventData)
    {
        if (eventData.pointerPressRaycast.gameObject != null)
            return eventData.pointerPressRaycast.gameObject.transform;

        return eventData.pointerCurrentRaycast.gameObject != null
            ? eventData.pointerCurrentRaycast.gameObject.transform
            : null;
    }

    private static Sprite SelectSprite(params Image[] candidates)
    {
        foreach (Image candidate in candidates)
        {
            if (candidate != null && candidate.sprite != null)
                return candidate.sprite;
        }

        return null;
    }

    private void HandleHidePixieDebuffEffectChanged(bool _)
    {
        RefreshVisuals();
    }

    private void HandleSkipGachaCrystalPopupChanged(bool _)
    {
        RefreshVisuals();
    }

    private void HandleUseManualBerserkerModeChanged(bool _)
    {
        RefreshVisuals();
    }
}
