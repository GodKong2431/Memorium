using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CachaSummonItemUI : MonoBehaviour
{
    [Header("Binding")]
    [SerializeField] private TextMeshProUGUI textTitle;
    [SerializeField] private Button buttonSummonOnce;
    [SerializeField] private Button buttonSummonTen;
    [SerializeField] private Button buttonSummonThirty;
    [SerializeField] private RectTransform summonExpRoot;
    [SerializeField] private TextMeshProUGUI textSummonLevel;
    [SerializeField] private TextMeshProUGUI textTicketAmount;

    private Action onSummonStateChanged;
    private CachaCrystalChangePopupUI crystalChangePopup;
    private CachaResultPopupUI resultPopup;
    private GachaType gachaType;
    private string summonTitle = string.Empty;
    private Slider summonExpSlider;

    private void Awake()
    {
        if (summonExpRoot != null)
            summonExpSlider = summonExpRoot.GetComponent<Slider>();

        if (buttonSummonOnce != null)
            buttonSummonOnce.onClick.AddListener(OnSummonOnceClicked);

        if (buttonSummonTen != null)
            buttonSummonTen.onClick.AddListener(OnSummonTenClicked);

        if (buttonSummonThirty != null)
            buttonSummonThirty.onClick.AddListener(OnSummonThirtyClicked);
    }

    private void OnDestroy()
    {
        if (buttonSummonOnce != null)
            buttonSummonOnce.onClick.RemoveListener(OnSummonOnceClicked);

        if (buttonSummonTen != null)
            buttonSummonTen.onClick.RemoveListener(OnSummonTenClicked);

        if (buttonSummonThirty != null)
            buttonSummonThirty.onClick.RemoveListener(OnSummonThirtyClicked);
    }

    public void Bind(
        GachaType targetGachaType,
        string targetTitle,
        CachaCrystalChangePopupUI targetPopup,
        CachaResultPopupUI targetResultPopup,
        Action onChanged)
    {
        gachaType = targetGachaType;
        summonTitle = targetTitle ?? string.Empty;
        crystalChangePopup = targetPopup;
        resultPopup = targetResultPopup;
        onSummonStateChanged = onChanged;

        RefreshUI();
    }

    public void RefreshUI()
    {
        if (textTitle != null)
            textTitle.text = summonTitle;

        GachaLevelState levelState = TryGetLevelState();
        if (textSummonLevel != null)
            textSummonLevel.text = levelState != null ? levelState.Level.ToString() : "0";

        if (summonExpSlider != null)
        {
            float progress01 = GetProgress01(levelState);
            summonExpSlider.minValue = 0f;
            summonExpSlider.maxValue = 1f;
            summonExpSlider.value = progress01;
            summonExpSlider.interactable = false;
        }

        if (textTicketAmount != null)
            textTicketAmount.text = GetTicketAmount().ToString("F0");

        RefreshButtonState(buttonSummonOnce, 1);
        RefreshButtonState(buttonSummonTen, 10);
        RefreshButtonState(buttonSummonThirty, 30);
    }

    private void OnSummonOnceClicked()
    {
        TrySummon(1);
    }

    private void OnSummonTenClicked()
    {
        TrySummon(10);
    }

    private void OnSummonThirtyClicked()
    {
        TrySummon(30);
    }

    private void RefreshButtonState(Button button, int drawCount)
    {
        if (button == null)
            return;

        bool isSupported = IsSupportedDrawCount(drawCount);
        if (button.gameObject.activeSelf != isSupported)
            button.gameObject.SetActive(isSupported);

        if (!isSupported)
            return;

        button.interactable = CanDraw(drawCount);
    }

    private bool TrySummon(int drawCount)
    {
        if (!CanDraw(drawCount))
            return false;

        GachaManager manager = GachaManager.Instance;
        if (manager == null)
            return false;

        if (manager.CanDrawWithTickets(gachaType, drawCount))
            return PerformSummon(drawCount);

        if (GameOptionSettings.SkipGachaCrystalPopup)
            return PerformSummon(drawCount);

        if (crystalChangePopup != null)
            return crystalChangePopup.Show(gachaType, summonTitle, drawCount, () => PerformSummon(drawCount));

        RefreshUI();
        return false;
    }

    private bool CanDraw(int drawCount)
    {
        if (!IsSupportedDrawCount(drawCount))
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad)
            return false;

        GachaManager manager = GachaManager.Instance;
        if (manager == null)
            return false;

        return manager.CanPurchaseAndDraw(gachaType, drawCount);
    }

    private bool PerformSummon(int drawCount)
    {
        GachaManager manager = GachaManager.Instance;
        if (manager == null)
            return false;

        if (!manager.TryPurchaseTicketsAndDraw(gachaType, drawCount, out GachaDrawResult result))
        {
            RefreshUI();
            return false;
        }

        onSummonStateChanged?.Invoke();

        if (resultPopup != null)
            resultPopup.Show(gachaType, drawCount, result, () => TrySummon(drawCount));

        return true;
    }

    private bool IsSupportedDrawCount(int drawCount)
    {
        switch (gachaType)
        {
            case GachaType.Weapon:
            case GachaType.Armor:
                return drawCount == 1 || drawCount == 10 || drawCount == 30;
            case GachaType.SkillScroll:
                return drawCount == 1 || drawCount == 10;
            default:
                return false;
        }
    }

    private GachaLevelState TryGetLevelState()
    {
        GachaManager manager = GachaManager.Instance;
        return manager != null ? manager.GetLevelState(gachaType) : null;
    }

    private BigDouble GetTicketAmount()
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        if (currencyModule == null)
            return BigDouble.Zero;

        return currencyModule.GetAmount(GetTicketCurrency(gachaType));
    }

    private static float GetProgress01(GachaLevelState levelState)
    {
        if (levelState == null)
            return 0f;

        if (levelState.IsMaxLevel)
            return 1f;

        return Mathf.Clamp01((float)levelState.DrawCountInCurrentLevel / GachaConfig.DrawsPerLevel);
    }

    private static CurrencyType GetTicketCurrency(GachaType targetGachaType)
    {
        switch (targetGachaType)
        {
            case GachaType.Weapon:
                return CurrencyType.WeaponDrawTicket;
            case GachaType.Armor:
                return CurrencyType.ArmorDrawTicket;
            case GachaType.SkillScroll:
                return CurrencyType.SkillScrollDrawTicket;
            default:
                return CurrencyType.WeaponDrawTicket;
        }
    }
}
