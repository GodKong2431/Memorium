using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 체력·골드·경험치·아이템 등 약식 데이터 저장.
/// 적 처치 보상 이벤트를 구독해 자동 반영.
/// </summary>
public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance { get; private set; }

    [Header("초기값")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private int startGold = 0;

    private float _currentHealth;
    private int _gold;
    private int _exp;
    private readonly Dictionary<string, int> _items = new Dictionary<string, int>();

    public float CurrentHealth => _currentHealth;
    public float MaxHealth => maxHealth;
    public int Gold => _gold;
    public int Exp => _exp;
    public IReadOnlyDictionary<string, int> Items => _items;

    public event Action OnDataChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _currentHealth = maxHealth;
        _gold = startGold;
        _exp = 0;
        _items.Clear();

        EnemyKillRewardDispatcher.OnGoldEarned += AddGold;
        EnemyKillRewardDispatcher.OnExpEarned += AddExp;
        EnemyKillRewardDispatcher.OnItemDropped += AddItem;
        EnemyKillRewardDispatcher.OnEquipmentDropped += AddEquipment;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EnemyKillRewardDispatcher.OnGoldEarned -= AddGold;
            EnemyKillRewardDispatcher.OnExpEarned -= AddExp;
            EnemyKillRewardDispatcher.OnItemDropped -= AddItem;
            EnemyKillRewardDispatcher.OnEquipmentDropped -= AddEquipment;
            Instance = null;
        }
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;
        _gold += amount;
        OnDataChanged?.Invoke();
    }

    public void AddExp(int amount)
    {
        if (amount <= 0) return;
        _exp += amount;
        OnDataChanged?.Invoke();
    }

    public void AddItem(string itemId, int count)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        _items.TryGetValue(itemId, out int current);
        _items[itemId] = current + count;
        OnDataChanged?.Invoke();
    }

    public void AddEquipment(string itemId, int count, int power)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;
        string key = $"{itemId}_{power}";
        _items.TryGetValue(key, out int current);
        _items[key] = current + count;
        OnDataChanged?.Invoke();
    }

    public void TakeDamage(float damage)
    {
        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        OnDataChanged?.Invoke();
    }

    public void Heal(float amount)
    {
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        OnDataChanged?.Invoke();
    }

    public int GetItemCount(string itemId)
    {
        return _items.TryGetValue(itemId, out int count) ? count : 0;
    }

    public string GetItemsSummary()
    {
        if (_items.Count == 0) return "-";
        var sb = new System.Text.StringBuilder();
        foreach (var kv in _items)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(kv.Key).Append(" x").Append(kv.Value);
        }
        return sb.ToString();
    }
}
