using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 가챠(뽑기) 시스템 메인 매니저.
/// 재화 소비, 뽑기 실행, 인벤토리 연동. (로직 우선 구현, UI 없음)
/// 
/// 주석을 최대한 적어봤습니다. 임시라고 써둔 것들은 UI 연동하시면서 지우셔도 무방합니다!
/// </summary>
public class GachaManager : Singleton<GachaManager>
{

    [SerializeField] private SerializedDictionary<GachaType, GachaLevelState> levelStates = new SerializedDictionary<GachaType, GachaLevelState>();

    private CurrencyInventoryModule CurrencyModule => InventoryManager.Instance?.GetModule<CurrencyInventoryModule>();

    public SaveGachaData saveGachaData;

    public int crystalId = 0;

    protected override void Awake()
    {
        StartCoroutine(CheckCrystalId());
        base.Awake();
        InitializeLevelStates();
    }

    IEnumerator CheckCrystalId()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        foreach (var item in DataManager.Instance.ItemInfoDict)
        {
            if (item.Value.itemType == ItemType.PaidCurrency)
            {
                crystalId = item.Key;
                break;
            }
        }
    }

    private void InitializeLevelStates()
    {
        levelStates.Clear();

        foreach (GachaType type in Enum.GetValues(typeof(GachaType)))
            levelStates[type] = new GachaLevelState(type, 1, 0);
    }

    public GachaLevelState GetLevelState(GachaType gachaType)
    {
        if (!levelStates.TryGetValue(gachaType, out GachaLevelState state))
        {
            state = new GachaLevelState(gachaType, 1, 0);
            levelStates[gachaType] = state;
        }

        return state;
    }

    /// <summary>1회 뽑기. 뽑기권 1장 또는 크리스탈 10개 소비.</summary>
    public bool TryDrawOnce(GachaType gachaType, out GachaDrawResult result)
    {
        return TryDraw(gachaType, 1, out result);
    }

    /// <summary>10회 뽑기. 뽑기권 10장 또는 크리스탈 100개 소비.</summary>
    public bool TryDrawTen(GachaType gachaType, out GachaDrawResult result)
    {
        return TryDraw(gachaType, 10, out result);
    }

    /// <summary>30회 뽑기. 뽑기권 30장 또는 크리스탈 300개 소비.</summary>
    public bool TryDrawBulk(GachaType gachaType, out GachaDrawResult result)
    {
        return TryDraw(gachaType, 30, out result);
    }

    /// <summary>뽑기권 부족 시 크리스탈으로 필요한 만큼 구매(1장=10 크리스탈) 후 뽑기 진행. 구매 및 사용 한 번에.</summary>
    public bool TryPurchaseTicketsAndDraw(GachaType gachaType, int drawCount, out GachaDrawResult result)
    {
        result = GachaDrawResult.Create();
        if (!IsEquipmentGacha(gachaType) && !IsSkillScrollGacha(gachaType)) return false;

        if (!IsSupportedDrawCount(gachaType, drawCount)) return false;

        CurrencyType ticketType = GetTicketCurrency(gachaType);
        int ticketNeeded = drawCount;
        BigDouble currentTickets = CurrencyModule?.GetAmount(ticketType) ?? BigDouble.Zero;

        if (currentTickets >= ticketNeeded)
            return TryDraw(gachaType, drawCount, out result);

        int ticketsToBuy = ticketNeeded - (int)currentTickets.ToDouble();
        int purchaseCount = ((ticketsToBuy - 1) / GachaConfig.TicketCostPerDraw + 1) * GachaConfig.TicketCostPerDraw;
        int cost = purchaseCount * GachaConfig.CrystalCostPerDraw;

        if (CurrencyModule == null || !CurrencyModule.HasEnough(CurrencyType.Crystal, new BigDouble(cost)))
            return false;

        InventoryManager.Instance.RemoveItem(crystalId, cost);
        CurrencyModule.AddCurrency(ticketType, new BigDouble(purchaseCount));

        if (!TryDraw(gachaType, drawCount, out result))
            return false;

        result.SpentCurrencies.TryGetValue(CurrencyType.Crystal, out int existing);
        result.SpentCurrencies[CurrencyType.Crystal] = existing + cost;
        return true;
    }

    public bool CanPurchaseAndDraw(GachaType gachaType, int drawCount)
    {
        return TryGetSpendPreview(gachaType, drawCount, out _, out _, out _, out _);
    }

    public bool TryGetSpendPreview(
        GachaType gachaType,
        int drawCount,
        out CurrencyType spendCurrencyType,
        out int spendAmount,
        out int ownedTicketCount,
        out int missingTicketCount)
    {
        spendCurrencyType = GetTicketCurrency(gachaType);
        spendAmount = 0;
        ownedTicketCount = 0;
        missingTicketCount = 0;

        if (!IsEquipmentGacha(gachaType) && !IsSkillScrollGacha(gachaType))
            return false;

        if (!IsSupportedDrawCount(gachaType, drawCount))
            return false;

        CurrencyInventoryModule currency = CurrencyModule;
        if (currency == null)
            return false;

        CurrencyType ticketType = GetTicketCurrency(gachaType);
        ownedTicketCount = Mathf.Max(0, (int)currency.GetAmount(ticketType).ToDouble());

        if (ownedTicketCount >= drawCount)
        {
            spendCurrencyType = ticketType;
            spendAmount = drawCount;
            return true;
        }

        int ticketsToBuy = drawCount - ownedTicketCount;
        missingTicketCount = ticketsToBuy;
        int purchaseCount = ((ticketsToBuy - 1) / GachaConfig.TicketCostPerDraw + 1) * GachaConfig.TicketCostPerDraw;
        int crystalCost = purchaseCount * GachaConfig.CrystalCostPerDraw;
        if (!currency.HasEnough(CurrencyType.Crystal, new BigDouble(crystalCost)))
            return false;

        spendCurrencyType = CurrencyType.Crystal;
        spendAmount = crystalCost;
        return true;
    }

    private bool TryDraw(GachaType gachaType, int count, out GachaDrawResult result)
    {
        result = GachaDrawResult.Create();
        if (count <= 0)
            return false;

        if (!IsSupportedDrawCount(gachaType, count))
            return false;

        if (!IsEquipmentGacha(gachaType) && !IsSkillScrollGacha(gachaType))
            return false;

        CurrencyInventoryModule currency = CurrencyModule;
        if (currency == null)
            return false;

        GachaLevelState state = GetLevelState(gachaType);

        CurrencyType ticketType = GetTicketCurrency(gachaType);
        int ticketCost = GetTicketCost(count);
        int crystalCost = GetCrystalCost(count);
        BigDouble ticketAmount = currency.GetAmount(ticketType);
        BigDouble crystalAmount = currency.GetAmount(CurrencyType.Crystal);

        if (ticketAmount >= ticketCost)
        {
            currency.TrySpend(ticketType, new BigDouble(ticketCost));
            result.SpentCurrencies[ticketType] = ticketCost;
        }
        else if (crystalAmount >= crystalCost)
        {
            InventoryManager.Instance.RemoveItem(crystalId, crystalCost);
            result.SpentCurrencies[CurrencyType.Crystal] = crystalCost;
        }
        else
        {
            return false;
        }

        int levelBefore = state.Level;
        for (int i = 0; i < count; i++)
        {
            if (IsSkillScrollGacha(gachaType))
            {
                var scrollIds = SkillScrollGachaLogic.DrawSkillScrolls(state.Level);
                foreach (int scrollId in scrollIds)
                {
                    if (scrollId <= 0)
                        continue;

                    result.ItemIds.Add(scrollId);
                    InventoryManager.Instance?.AddItem(scrollId, 1);
                }

                state.AddDraws(1);
                continue;
            }

            EquipmentGachaLogic.DrawResult drawResult = gachaType == GachaType.Weapon
                ? EquipmentGachaLogic.DrawWeapon(state.Stage)
                : EquipmentGachaLogic.DrawArmor(state.Stage);

            if (drawResult.ItemId > 0)
            {
                result.ItemIds.Add(drawResult.ItemId);
                if (drawResult.IsRare)
                    result.HasRareItem = true;

                InventoryManager.Instance?.AddItem(drawResult.ItemId, 1);
            }

            state.AddDraws(1);
        }

        result.LevelUp = state.Level > levelBefore;
        return true;
    }

    private static bool IsEquipmentGacha(GachaType type)
    {
        return type == GachaType.Weapon || type == GachaType.Armor;
    }

    private static bool IsSkillScrollGacha(GachaType type)
    {
        return type == GachaType.SkillScroll;
    }

    private static bool IsSupportedDrawCount(GachaType gachaType, int drawCount)
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

    private static int GetTicketCost(int count)
    {
        return count * GachaConfig.TicketCostPerDraw;
    }

    private static int GetCrystalCost(int count)
    {
        return count * GachaConfig.CrystalCostPerDraw;
    }

    private static CurrencyType GetTicketCurrency(GachaType gachaType)
    {
        switch (gachaType)
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

    /// <summary>뽑기권으로 N회 뽑기 가능한지 확인</summary>
    public bool CanDrawWithTickets(GachaType gachaType, int count)
    {
        CurrencyType ticketType = GetTicketCurrency(gachaType);
        int cost = GetTicketCost(count);
        return cost > 0 && (CurrencyModule?.GetAmount(ticketType) ?? BigDouble.Zero) >= new BigDouble(cost);
    }

    /// <summary>크리스탈으로 N회 뽑기 가능한지 확인 (티켓 부족 시 대체)</summary>
    public bool CanDrawWithCrystal(GachaType gachaType, int count)
    {
        int cost = GetCrystalCost(count);
        return cost > 0 && (CurrencyModule?.GetAmount(CurrencyType.Crystal) ?? BigDouble.Zero) >= new BigDouble(cost);
    }

    /// <summary>30회 벌크 뽑기 가능한지 확인. 스킬 주문서는 기획상 1회/10회만 지원.</summary>
    public bool CanDrawBulk(GachaType gachaType)
    {
        if (IsSkillScrollGacha(gachaType))
            return false;

        return CanPurchaseAndDraw(gachaType, 30);
    }

    public bool CanPurchaseTickets(GachaType gachaType)
    {
        return (CurrencyModule?.GetAmount(CurrencyType.Crystal) ?? BigDouble.Zero) >= new BigDouble(GachaConfig.CrystalCostPerDraw);
    }
}
