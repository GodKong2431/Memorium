using UnityEngine;

public class DungeonTicketUIController : UIControllerBase
{
    [SerializeField] private DungeonTicketUIView view;
    [SerializeField] private DungeonLevelSelectUI dungeonLevelSelectUI; // 현재 선택된 던전 정보와 필요 티켓 수를 제공하는 대상.

    protected override void Initialize()
    {
        if (view == null)
            view = GetComponentInChildren<DungeonTicketUIView>(true);

        if (dungeonLevelSelectUI == null)
            dungeonLevelSelectUI = GetComponentInParent<DungeonLevelSelectUI>(true);
    }

    // 재화 변화와 던전 선택 상태 변화를 모두 구독한다.
    protected override void Subscribe()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;

        if (dungeonLevelSelectUI != null)
            dungeonLevelSelectUI.TicketStateChanged += RefreshView;
    }

    // 연결했던 이벤트를 모두 해제한다.
    protected override void Unsubscribe()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;

        if (dungeonLevelSelectUI != null)
            dungeonLevelSelectUI.TicketStateChanged -= RefreshView;
    }

    // 현재 던전 기준의 보유 티켓 수와 필요 티켓 수를 View에 반영한다.
    protected override void RefreshView()
    {
        if (view == null || dungeonLevelSelectUI == null)
            return;

        BigDouble ownedAmount = GetAmount(dungeonLevelSelectUI.CurrentTicketType);
        view.SetTicketInfo(ownedAmount, dungeonLevelSelectUI.RequiredTicketCount);
    }

    // 현재 던전에서 사용하는 티켓 재화가 변경됐을 때만 UI를 갱신한다.
    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (dungeonLevelSelectUI == null || type != dungeonLevelSelectUI.CurrentTicketType)
            return;

        RefreshView();
    }

    private static BigDouble GetAmount(CurrencyType type)
    {
        if (InventoryManager.Instance == null)
            return BigDouble.Zero;

        CurrencyInventoryModule currencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
        return currencyModule != null ? currencyModule.GetAmount(type) : BigDouble.Zero;
    }
}
