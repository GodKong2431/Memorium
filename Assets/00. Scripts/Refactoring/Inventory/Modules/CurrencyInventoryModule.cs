using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class CurrencyInventoryModule : IInventoryModule
{
    private readonly Dictionary<CurrencyType, BigDouble> amountByCurrency = new Dictionary<CurrencyType, BigDouble>(); // 재화 타입별 보유량.
    
    public event Action<CurrencyType, BigDouble> OnCurrencyChanged; // 재화 변경을 외부 시스템에 전달하는 이벤트.

    public CurrencyInventoryModule()
    {
        foreach (CurrencyType type in Enum.GetValues(typeof(CurrencyType)))
            amountByCurrency[type] = BigDouble.Zero;
    }

    #region IInventoryModule
    // 전달된 ItemType이 이 모듈의 처리 대상인지 확인한다.
    public bool CanHandle(ItemType itemType)
    {
        return InventoryTypeMapper.TryToCurrencyType(itemType, out _);
    }

    // ItemType 기반 추가 요청을 CurrencyType으로 변환해 처리한다.
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!InventoryTypeMapper.TryToCurrencyType(item.ItemType, out var currencyType))
            return false;

        AddCurrency(currencyType, amount);
        return true;
    }

    // ItemType 기반 차감 요청을 CurrencyType으로 변환해 처리한다.
    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        if (!InventoryTypeMapper.TryToCurrencyType(item.ItemType, out var currencyType))
            return false;

        return TrySpend(currencyType, amount);
    }

    // ItemType 기반 재화 수량 조회를 처리한다.
    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!InventoryTypeMapper.TryToCurrencyType(item.ItemType, out var currencyType))
            return BigDouble.Zero;

        return GetAmount(currencyType);
    }
    #endregion

    // CurrencyType 기반 재화 수량을 반환한다.
    public BigDouble GetAmount(CurrencyType currencyType)
    {
        EnsureCurrencyKey(currencyType);
        return amountByCurrency[currencyType];
    }

    // 요구 수량 이상을 보유했는지 확인한다.
    public bool HasEnough(CurrencyType currencyType, BigDouble requiredAmount)
    {
        return GetAmount(currencyType) >= requiredAmount;
    }

    // 재화를 증가시키고 이벤트를 발행한다.
    public void AddCurrency(CurrencyType currencyType, BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return;

        EnsureCurrencyKey(currencyType);
        amountByCurrency[currencyType] = amountByCurrency[currencyType] + amount;
        PublishCurrencyChanged(currencyType);
    }

    // 재화를 차감하고 이벤트를 발행한다.
    public bool TrySpend(CurrencyType currencyType, BigDouble cost)
    {
        if (cost <= BigDouble.Zero)
            return true;

        if (!HasEnough(currencyType, cost))
            return false;

        EnsureCurrencyKey(currencyType);
        amountByCurrency[currencyType] = amountByCurrency[currencyType] - cost;
        PublishCurrencyChanged(currencyType);
        return true;
    }

    // 재화를 차감하지만 이벤트는 발행하지 않는다.
    public bool TrySpendSilent(CurrencyType currencyType, BigDouble cost)
    {
        if (cost <= BigDouble.Zero)
            return true;

        if (!HasEnough(currencyType, cost))
            return false;

        EnsureCurrencyKey(currencyType);
        amountByCurrency[currencyType] = amountByCurrency[currencyType] - cost;
        return true;
    }

    // 딕셔너리에 재화 키가 없으면 0으로 초기화한다.
    private void EnsureCurrencyKey(CurrencyType currencyType)
    {
        if (!amountByCurrency.ContainsKey(currencyType))
            amountByCurrency[currencyType] = BigDouble.Zero;
    }

    // 재화 변경 이벤트를 내부/외부 시스템에 동시에 전파한다.
    private void PublishCurrencyChanged(CurrencyType currencyType)
    {
        BigDouble currentAmount = amountByCurrency[currencyType];
        OnCurrencyChanged?.Invoke(currencyType, currentAmount);
        GameEventManager.OnCurrencyChanged?.Invoke(currencyType, currentAmount);
    }
}
