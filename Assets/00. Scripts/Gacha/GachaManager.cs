using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    private void Update()
    {
        // 임시: G키로 30회 벌크 뽑기 테스트 (UI 연동 전)
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
        {
            //InventoryManager.Instance?.GetModule<CurrencyInventoryModule>()?.AddCurrency(CurrencyType.Crystal, new BigDouble(30 * GachaConfig.CrystalCostPerDraw));
            var gachaType = GachaType.Weapon;
            if (TryDrawBulk(gachaType, out var result))
            {
                LogDrawResult(gachaType, true, result);
            }
            else
            {
                LogDrawResult(gachaType, false, result);
            }
        }
    }

    /// <summary>UI 연동 전 임시: 뽑기 결과를 디버그 로그로 출력</summary>
    private void LogDrawResult(GachaType gachaType, bool success, GachaDrawResult result)
    {
        if (success)
        {
            var spentStr = string.Join(", ", result.SpentCurrencies.Select(kv => $"{kv.Key}:{kv.Value}"));
            var itemsStr = result.ItemIds.Count > 0 ? string.Join(", ", result.ItemIds) : "(없음)";
            Debug.Log($"[Gacha] {gachaType} 30회 벌크 뽑기 성공 | ItemIds: [{itemsStr}] | 소비: {spentStr} | 레벨업: {result.LevelUp} | 레어: {result.HasRareItem}");
        }
        else
        {
            Debug.LogWarning($"[Gacha] {gachaType} 30회 벌크 뽑기 실패 (재화 부족 또는 조건 미충족)");
        }
    }

    /// <summary>가챠 유형별 레벨 상태 초기화 (아마 나중에 저장하는 기능 연동 필요)</summary>
    private void InitializeLevelStates()
    {
        //saveGachaData = JSONService.Load<SaveGachaData>();
        //saveGachaData.InitGachaData();
        //foreach (GachaType type in Enum.GetValues(typeof(GachaType)))
        //{
        //    if (!levelStates.ContainsKey(type))
        //        levelStates[type] = saveGachaData.GetGachaData(type);
        //        //levelStates[type] = new GachaLevelState { GachaType = type };
        //}
    }

    /// <summary>가챠 유형별 레벨 상태 조회</summary>
    public GachaLevelState GetLevelState(GachaType gachaType)
    {
        if (!levelStates.TryGetValue(gachaType, out var state))
        {
            //state = new GachaLevelState { GachaType = gachaType };
            state = saveGachaData.GetGachaData(gachaType);
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
        if (!IsEquipmentGacha(gachaType)) return false;

        var ticketType = GetTicketCurrency(gachaType);
        int ticketNeeded = drawCount;
        BigDouble currentTickets = CurrencyModule?.GetAmount(ticketType) ?? BigDouble.Zero;

        if (currentTickets >= ticketNeeded)
            return TryDraw(gachaType, drawCount, out result);

        int ticketsToBuy = ticketNeeded - (int)currentTickets.ToDouble();
        int purchaseCount = ((ticketsToBuy - 1) / GachaConfig.TicketCostPerDraw + 1) * GachaConfig.TicketCostPerDraw;
        int cost = purchaseCount * GachaConfig.CrystalCostPerDraw;

        if (CurrencyModule == null || !CurrencyModule.HasEnough(CurrencyType.Crystal, new BigDouble(cost)))
            return false;

        //CurrencyModule.TrySpend(CurrencyType.Crystal, new BigDouble(cost));
        InventoryManager.Instance.RemoveItem(crystalId, cost);

        CurrencyModule.AddCurrency(ticketType, new BigDouble(purchaseCount));

        if (!TryDraw(gachaType, drawCount, out result))
            return false;

        result.SpentCurrencies.TryGetValue(CurrencyType.Crystal, out int existing);
        result.SpentCurrencies[CurrencyType.Crystal] = existing + cost;
        return true;
    }

    /// <summary>핵심 뽑기 로직. 뽑기권 또는 크리스탈 소비 후 뽑기 실행.</summary>
    private bool TryDraw(GachaType gachaType, int count, out GachaDrawResult result)
    {
        result = GachaDrawResult.Create();
        if (count <= 0 || !IsEquipmentGacha(gachaType)) return false;

        var currency = CurrencyModule;
        if (currency == null) return false;

        var state = GetLevelState(gachaType);

        // 1. 재화 차감 (뽑기권 우선, 부족 시 크리스탈)
        var ticketType = GetTicketCurrency(gachaType);
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
            //currency.TrySpend(CurrencyType.Crystal, new BigDouble(crystalCost));
            InventoryManager.Instance.RemoveItem(crystalId, crystalCost);
            result.SpentCurrencies[CurrencyType.Crystal] = crystalCost;
        }
        else
            return false;

        // 2. 뽑기 실행 및 인벤토리 추가
        int levelBefore = state.Level;
        for (int i = 0; i < count; i++)
        {
            var drawResult = gachaType == GachaType.Weapon
                ? EquipmentGachaLogic.DrawWeapon(state.Stage)
                : EquipmentGachaLogic.DrawArmor(state.Stage);

            if (drawResult.ItemId > 0)
            {
                result.ItemIds.Add(drawResult.ItemId);
                if (drawResult.IsRare) result.HasRareItem = true;
                InventoryManager.Instance?.AddItem(drawResult.ItemId, 1);
            }

            state.AddDraws(1);
        }

        result.LevelUp = state.Level > levelBefore;

        saveGachaData.SaveGachaLevel(gachaType, state.Level, state.DrawCountInCurrentLevel);
        return true;
    }

    private static bool IsEquipmentGacha(GachaType type) => type == GachaType.Weapon || type == GachaType.Armor;

    private static int GetTicketCost(int count) => count * GachaConfig.TicketCostPerDraw;
    private static int GetCrystalCost(int count) => count * GachaConfig.CrystalCostPerDraw;

    private static CurrencyType GetTicketCurrency(GachaType gachaType)
    {
        return gachaType == GachaType.Weapon ? CurrencyType.WeaponDrawTicket : CurrencyType.ArmorDrawTicket;
    }

    /// <summary>뽑기권으로 N회 뽑기 가능한지 확인</summary>
    public bool CanDrawWithTickets(GachaType gachaType, int count)
    {
        var ticketType = GetTicketCurrency(gachaType);
        int cost = GetTicketCost(count);
        return cost > 0 && (CurrencyModule?.GetAmount(ticketType) ?? BigDouble.Zero) >= new BigDouble(cost);
    }

    /// <summary>크리스탈으로 N회 뽑기 가능한지 확인 (티켓 부족 시 대체)</summary>
    public bool CanDrawWithCrystal(GachaType gachaType, int count)
    {
        int cost = GetCrystalCost(count);
        return cost > 0 && (CurrencyModule?.GetAmount(CurrencyType.Crystal) ?? BigDouble.Zero) >= new BigDouble(cost);
    }

    /// <summary>30회 벌크 뽑기 가능한지 확인 (뽑기권 30장 또는 크리스탈 300개)</summary>
    public bool CanDrawBulk(GachaType gachaType)
    {
        return CanDrawWithTickets(gachaType, 30) || CanDrawWithCrystal(gachaType, 30);
    }

    /// <summary>뽑기권 1장 구매 가능한지 확인 (크리스탈 10 필요)</summary>
    public bool CanPurchaseTickets(GachaType gachaType)
    {
        return (CurrencyModule?.GetAmount(CurrencyType.Crystal) ?? BigDouble.Zero) >= new BigDouble(GachaConfig.CrystalCostPerDraw);
    }

    protected override void OnApplicationQuit()
    {
        JSONService.Save(saveGachaData);
    }
}
