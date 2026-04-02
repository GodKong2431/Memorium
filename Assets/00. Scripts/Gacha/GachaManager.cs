using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using UnityEngine;

public class GachaManager : Singleton<GachaManager>
{
    [SerializeField] private SerializedDictionary<GachaType, GachaLevelState> levelStates = new SerializedDictionary<GachaType, GachaLevelState>();

    private CurrencyInventoryModule CurrencyModule => InventoryManager.Instance?.GetModule<CurrencyInventoryModule>();

    public SaveGachaData saveGachaData;
    public int crystalId = 0;
    public bool DataLoad = false;

    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(LoadData());
    }

    private IEnumerator LoadData()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        ResolveCrystalId();

        yield return new WaitUntil(() => InventoryManager.Instance != null);
        yield return new WaitUntil(() => InventoryManager.Instance.DataLoad);

        saveGachaData = JSONService.Load<SaveGachaData>() ?? new SaveGachaData();
        saveGachaData.InitGachaData();

        levelStates.Clear();
        foreach (GachaType type in Enum.GetValues(typeof(GachaType)))
            levelStates[type] = saveGachaData.GetGachaData(type);

        MigrateLegacyTicketCurrencies();
        DataLoad = true;
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

    public bool TryDrawOnce(GachaType gachaType, out GachaDrawResult result)
    {
        return TryDraw(gachaType, 1, out result);
    }

    public bool TryDrawTen(GachaType gachaType, out GachaDrawResult result)
    {
        return TryDraw(gachaType, 10, out result);
    }

    public bool TryDrawBulk(GachaType gachaType, out GachaDrawResult result)
    {
        return TryDraw(gachaType, 30, out result);
    }

    public bool TryPurchaseTicketsAndDraw(GachaType gachaType, int drawCount, out GachaDrawResult result)
    {
        result = GachaDrawResult.Create();

        if (!DataLoad)
            return false;

        if (!IsEquipmentGacha(gachaType) && !IsSkillScrollGacha(gachaType))
            return false;

        if (!IsSupportedDrawCount(gachaType, drawCount))
            return false;

        int ticketItemId = GachaTicketResolver.GetTicketItemId(gachaType);
        if (ticketItemId <= 0)
            return false;

        int ticketNeeded = drawCount;
        BigDouble currentTickets = GachaTicketResolver.GetOwnedTicketAmount(gachaType);

        if (currentTickets >= ticketNeeded)
            return TryDraw(gachaType, drawCount, out result);

        int ticketsToBuy = ticketNeeded - Mathf.Max(0, (int)currentTickets.ToDouble());
        int purchaseCount = ((ticketsToBuy - 1) / GachaConfig.TicketCostPerDraw + 1) * GachaConfig.TicketCostPerDraw;
        int crystalCost = purchaseCount * GachaConfig.CrystalCostPerDraw;

        if (CurrencyModule == null || crystalId <= 0)
            return false;

        if (!CurrencyModule.HasEnough(CurrencyType.Crystal, new BigDouble(crystalCost)))
            return false;

        if (!InventoryManager.Instance.RemoveItem(crystalId, crystalCost))
            return false;

        if (!InventoryManager.Instance.AddItem(ticketItemId, purchaseCount))
        {
            InventoryManager.Instance.AddItem(crystalId, crystalCost);
            return false;
        }

        if (!TryDraw(gachaType, drawCount, out result))
            return false;

        result.SpentCurrencies.TryGetValue(CurrencyType.Crystal, out int existingCrystalSpend);
        result.SpentCurrencies[CurrencyType.Crystal] = existingCrystalSpend + crystalCost;
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
        spendCurrencyType = CurrencyType.GachaTicket;
        spendAmount = 0;
        ownedTicketCount = 0;
        missingTicketCount = 0;

        if (!DataLoad)
            return false;

        if (!IsEquipmentGacha(gachaType) && !IsSkillScrollGacha(gachaType))
            return false;

        if (!IsSupportedDrawCount(gachaType, drawCount))
            return false;

        if (InventoryManager.Instance == null || !InventoryManager.Instance.DataLoad)
            return false;

        CurrencyInventoryModule currency = CurrencyModule;
        if (currency == null)
            return false;

        ownedTicketCount = Mathf.Max(0, (int)GachaTicketResolver.GetOwnedTicketAmount(gachaType).ToDouble());
        if (ownedTicketCount >= drawCount)
        {
            spendCurrencyType = CurrencyType.GachaTicket;
            spendAmount = drawCount;
            return true;
        }

        int ticketsToBuy = drawCount - ownedTicketCount;
        missingTicketCount = ticketsToBuy;

        int purchaseCount = ((ticketsToBuy - 1) / GachaConfig.TicketCostPerDraw + 1) * GachaConfig.TicketCostPerDraw;
        int crystalCost = purchaseCount * GachaConfig.CrystalCostPerDraw;
        if (crystalId <= 0 || !currency.HasEnough(CurrencyType.Crystal, new BigDouble(crystalCost)))
            return false;

        spendCurrencyType = CurrencyType.Crystal;
        spendAmount = crystalCost;
        return true;
    }

    private bool TryDraw(GachaType gachaType, int count, out GachaDrawResult result)
    {
        result = GachaDrawResult.Create();

        if (!DataLoad || count <= 0)
            return false;

        if (!IsSupportedDrawCount(gachaType, count))
            return false;

        if (!IsEquipmentGacha(gachaType) && !IsSkillScrollGacha(gachaType))
            return false;

        CurrencyInventoryModule currency = CurrencyModule;
        if (currency == null)
            return false;

        int ticketItemId = GachaTicketResolver.GetTicketItemId(gachaType);
        if (ticketItemId <= 0)
            return false;

        GachaLevelState state = GetLevelState(gachaType);
        int ticketCost = GetTicketCost(count);
        int crystalCost = GetCrystalCost(count);
        BigDouble ticketAmount = GachaTicketResolver.GetOwnedTicketAmount(gachaType);
        BigDouble crystalAmount = currency.GetAmount(CurrencyType.Crystal);

        if (ticketAmount >= ticketCost)
        {
            if (!InventoryManager.Instance.RemoveItem(ticketItemId, ticketCost))
                return false;

            result.SpentCurrencies[CurrencyType.GachaTicket] = ticketCost;
        }
        else if (crystalId > 0 && crystalAmount >= crystalCost)
        {
            if (!InventoryManager.Instance.RemoveItem(crystalId, crystalCost))
                return false;

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
                SkillScrollGachaLogic.DrawRoll scrollRoll = SkillScrollGachaLogic.DrawSkillScrollRoll(state.Level);
                if (scrollRoll.ItemId > 0 && scrollRoll.Count > 0)
                {
                    result.ItemIds.Add(scrollRoll.ItemId);
                    result.ItemCounts.Add(scrollRoll.Count);
                    result.ItemRareFlags.Add(false);
                    InventoryManager.Instance?.AddItem(scrollRoll.ItemId, scrollRoll.Count);
                }

                state.AddDraws(1);
                continue;
            }

            state.AddDraws(1);

            bool forcePity = state.ShouldForcePityThisDraw();
            EquipmentGachaLogic.DrawResult drawResult = gachaType == GachaType.Weapon
                ? EquipmentGachaLogic.DrawWeapon(state.Stage, forcePity)
                : EquipmentGachaLogic.DrawArmor(state.Stage, forcePity);

            if (drawResult.ItemId > 0)
            {
                result.ItemIds.Add(drawResult.ItemId);
                result.ItemCounts.Add(1);
                result.ItemRareFlags.Add(drawResult.IsRare);
                if (drawResult.IsRare)
                    result.HasRareItem = true;

                InventoryManager.Instance?.AddItem(drawResult.ItemId, 1);
            }

            state.UpdatePityAfterEquipmentDraw(drawResult.IsHighestTier);
        }

        result.LevelUp = state.Level > levelBefore;
        saveGachaData.SaveGachaLevel(gachaType, state.Level, state.DrawCountInCurrentLevel, state.PityCount);
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

    public bool CanDrawWithTickets(GachaType gachaType, int count)
    {
        if (!DataLoad)
            return false;

        int cost = GetTicketCost(count);
        return cost > 0 && GachaTicketResolver.GetOwnedTicketAmount(gachaType) >= new BigDouble(cost);
    }

    public bool CanDrawWithCrystal(GachaType gachaType, int count)
    {
        if (!DataLoad || crystalId <= 0)
            return false;

        int cost = GetCrystalCost(count);
        return cost > 0 && (CurrencyModule?.GetAmount(CurrencyType.Crystal) ?? BigDouble.Zero) >= new BigDouble(cost);
    }

    public bool CanDrawBulk(GachaType gachaType)
    {
        if (IsSkillScrollGacha(gachaType))
            return false;

        return CanPurchaseAndDraw(gachaType, 30);
    }

    public bool CanPurchaseTickets(GachaType gachaType)
    {
        return DataLoad
            && crystalId > 0
            && (CurrencyModule?.GetAmount(CurrencyType.Crystal) ?? BigDouble.Zero) >= new BigDouble(GachaConfig.CrystalCostPerDraw);
    }

    private void ResolveCrystalId()
    {
        crystalId = 0;

        if (DataManager.Instance?.ItemInfoDict == null)
            return;

        foreach (var item in DataManager.Instance.ItemInfoDict)
        {
            if (item.Value.itemType != ItemType.PaidCurrency)
                continue;

            crystalId = item.Key;
            return;
        }
    }

    private void MigrateLegacyTicketCurrencies()
    {
        CurrencyInventoryModule currency = CurrencyModule;
        InventoryManager inventory = InventoryManager.Instance;
        if (currency == null || inventory == null)
            return;

        MigrateLegacyTicketCurrency(GachaType.Weapon, currency, inventory);
        MigrateLegacyTicketCurrency(GachaType.Armor, currency, inventory);
        MigrateLegacyTicketCurrency(GachaType.SkillScroll, currency, inventory);
    }

    private void MigrateLegacyTicketCurrency(GachaType gachaType, CurrencyInventoryModule currency, InventoryManager inventory)
    {
        int ticketItemId = GachaTicketResolver.GetTicketItemId(gachaType);
        if (ticketItemId <= 0)
            return;

        CurrencyType legacyCurrencyType = GachaTicketResolver.GetLegacyCurrencyType(gachaType);
        BigDouble legacyAmount = currency.GetAmount(legacyCurrencyType);
        if (legacyAmount <= BigDouble.Zero)
            return;

        if (!currency.TrySpendSilent(legacyCurrencyType, legacyAmount))
            return;

        if (!inventory.AddItem(ticketItemId, legacyAmount))
        {
            currency.AddCurrency(legacyCurrencyType, legacyAmount);
            return;
        }

        inventory.saveCurrencyData?.Save(legacyCurrencyType, BigDouble.Zero);
        Debug.Log($"[GachaManager] Migrated legacy {legacyCurrencyType} to ticket item {ticketItemId}: {legacyAmount}");
    }
}
