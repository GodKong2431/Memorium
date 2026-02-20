using System;
using System.Collections.Generic;
using UnityEngine;


public class CurrencyManager : Singleton<CurrencyManager>
{
    private Dictionary<CurrencyType, BigDouble> currencies;
    public event Action<CurrencyType> OnCurrencyChanged;

    protected override void Awake()
    {
        base.Awake();
        currencies = new Dictionary<CurrencyType, BigDouble>();
        for (int i = 0; i < (int)CurrencyType.Count; i++)
        {
            currencies[(CurrencyType)i] = new BigDouble(0);
        }
    }

    public BigDouble GetAmount(CurrencyType type)
    {
        if (currencies.TryGetValue(type, out var amount))
            return amount;
        return new BigDouble(0);
    }

    public bool HasEnough(CurrencyType type, BigDouble cost)
    {
        return GetAmount(type) >= cost;
    }

    public bool TrySpend(CurrencyType type, BigDouble cost)
    {
        if (!HasEnough(type, cost))
        {
            Debug.Log($"{type}재화 부족");
            return false;

        }
        currencies[type] = currencies[type] - cost;
        OnCurrencyChanged?.Invoke(type);
        return true;
    }

    public void Add(CurrencyType type, BigDouble amount)
    {
        currencies[type] = GetAmount(type) + amount;
        OnCurrencyChanged?.Invoke(type);
    }
}