using UnityEngine;

public class DungeonTicketUIController : UIControllerBase<DungeonTicketUIView>
{
    [SerializeField] private DungeonLevelSelectUI dungeonLevelSelectUI; // 현재 선택된 던전 정보와 필요 티켓 수를 제공하는 대상.

    private CurrencyUIService currencyService; // 재화 변화 감시와 보유량 조회를 담당하는 전역 서비스.

    // 컨트롤러가 사용할 전역 재화 서비스를 준비한다.
    protected override void Initialize()
    {
        currencyService = CurrencyUIService.Instance;

        if (dungeonLevelSelectUI == null)
            dungeonLevelSelectUI = GetComponentInParent<DungeonLevelSelectUI>(true);
    }

    // 재화 변화와 던전 선택 상태 변화를 모두 구독한다.
    protected override void Subscribe()
    {
        if (currencyService == null)
            return;

        currencyService.Bind();
        currencyService.CurrencyChanged += OnCurrencyChanged;

        if (dungeonLevelSelectUI != null)
            dungeonLevelSelectUI.TicketStateChanged += RefreshView;
    }

    // 연결했던 이벤트를 모두 해제한다.
    protected override void Unsubscribe()
    {
        if (currencyService != null)
        {
            currencyService.CurrencyChanged -= OnCurrencyChanged;
            currencyService.Unbind();
        }

        if (dungeonLevelSelectUI != null)
            dungeonLevelSelectUI.TicketStateChanged -= RefreshView;
    }

    // 현재 던전 기준의 보유 티켓 수와 필요 티켓 수를 View에 반영한다.
    protected override void RefreshView()
    {
        if (view == null || dungeonLevelSelectUI == null || currencyService == null)
            return;

        BigDouble ownedAmount = currencyService.GetAmount(dungeonLevelSelectUI.CurrentTicketType);
        view.SetTicketInfo(ownedAmount, dungeonLevelSelectUI.RequiredTicketCount);
    }

    // 현재 던전에서 사용하는 티켓 재화가 변경됐을 때만 UI를 갱신한다.
    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (dungeonLevelSelectUI == null || type != dungeonLevelSelectUI.CurrentTicketType)
            return;

        RefreshView();
    }
}
