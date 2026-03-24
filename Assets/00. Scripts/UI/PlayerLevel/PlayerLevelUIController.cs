using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLevelUIController : UIControllerBase
{
    [Header("Player Level Binding")]
    [SerializeField] private TextMeshProUGUI textProgress;
    [SerializeField] private Slider sliderExpBar;
    [SerializeField] private TextMeshProUGUI textPlayerLevel;

    private PlayerLevelUIView playerLevelView;
    private PlayerLevel subscribedLevel;
    private Coroutine delayedRefreshRoutine;

    protected override void Initialize()
    {
        playerLevelView = new PlayerLevelUIView(
            textProgress,
            sliderExpBar,
            textPlayerLevel);
    }

    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        TrySubscribeLevelUp();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
        UnsubscribeLevelUp();

        if (delayedRefreshRoutine != null)
        {
            StopCoroutine(delayedRefreshRoutine);
            delayedRefreshRoutine = null;
        }
    }

    protected override void RefreshView()
    {
        TrySubscribeLevelUp();

        if (!TryGetLevelProgress(out int level, out BigDouble currentExp, out BigDouble requiredExp))
        {
            playerLevelView.Render(0, 0f);
            return;
        }

        float progress01 = requiredExp > BigDouble.Zero
            ? Mathf.Clamp01((currentExp / requiredExp).ToFloat())
            : 0f;

        playerLevelView.Render(level, progress01);
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type != CurrencyType.Exp)
            return;

        if (delayedRefreshRoutine != null)
            StopCoroutine(delayedRefreshRoutine);

        delayedRefreshRoutine = StartCoroutine(RefreshNextFrame());
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        RefreshView();
        delayedRefreshRoutine = null;
    }

    private void OnLevelUp()
    {
        RefreshView();
    }

    private void TrySubscribeLevelUp()
    {
        PlayerLevel currentLevel = CharacterStatManager.Instance != null
            ? CharacterStatManager.Instance.LevelBonus
            : null;

        if (currentLevel == null || subscribedLevel == currentLevel)
            return;

        UnsubscribeLevelUp();
        subscribedLevel = currentLevel;
        subscribedLevel.OnLevelUp += OnLevelUp;
    }

    private void UnsubscribeLevelUp()
    {
        if (subscribedLevel == null)
            return;

        subscribedLevel.OnLevelUp -= OnLevelUp;
        subscribedLevel = null;
    }

    private static bool TryGetLevelProgress(out int level, out BigDouble currentExp, out BigDouble requiredExp)
    {
        level = 0;
        currentExp = BigDouble.Zero;
        requiredExp = BigDouble.Zero;

        if (CharacterStatManager.Instance == null || !CharacterStatManager.Instance.TableLoad || CharacterStatManager.Instance.LevelBonus == null)
            return false;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currencyModule == null)
            return false;

        PlayerLevel levelData = CharacterStatManager.Instance.LevelBonus;
        level = levelData.CurrentLevel;
        requiredExp = levelData.RequiredExp;
        currentExp = currencyModule.GetAmount(CurrencyType.Exp);
        return true;
    }
}
