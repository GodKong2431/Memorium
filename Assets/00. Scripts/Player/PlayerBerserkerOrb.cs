using System;
using UnityEngine;

/// <summary>
/// 플레이어 버서커 오브 관리. 적 처치 시 EnemyKillRewardDispatcher.OnBerserkerOrbEarned 구독.
/// 스킬 UI 등 다른 시스템 초기화 후 실행되도록 ExecutionOrder 지연.
/// </summary>
[DefaultExecutionOrder(100)]
public class PlayerBerserkerOrb : MonoBehaviour
{
    public static PlayerBerserkerOrb Instance { get; private set; }

    public static int MaxBerserkerOrb{get; private set;}

    public static int NormalBerserkerOrb{get; private set;}
    public static int BossBerserkerOrb{get; private set;}


    private int _berserkerOrb;

    public int BerserkerOrb => _berserkerOrb;

    public event Action OnBerserkerOrbChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnemyKillRewardDispatcher.OnBerserkerOrbEarned += AddBerserkerOrb;
    }

    public void Init(BerserkmodeManageTable table)
    {
        MaxBerserkerOrb = table.berserkCounter;
        Debug.Log(MaxBerserkerOrb);
        NormalBerserkerOrb = table.normalDropQty;
        Debug.Log(NormalBerserkerOrb);
        BossBerserkerOrb = table.bossDropQty;
        Debug.Log(BossBerserkerOrb);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            EnemyKillRewardDispatcher.OnBerserkerOrbEarned -= AddBerserkerOrb;
            Instance = null;
        }
    }

    public void AddBerserkerOrb(int amount)
    {
        if (amount <= 0) return;
        _berserkerOrb = Mathf.Min(MaxBerserkerOrb, _berserkerOrb + amount);
        OnBerserkerOrbChanged?.Invoke();
        BerserkerGageUI.RefreshAll();
    }

    /// <summary>버서커 모드 발동 시 오브 소모. 보유량이 부족하면 false.</summary>
    public bool TryConsumeBerserkerOrbs(int amount)
    {
        if (amount <= 0 || _berserkerOrb < amount) return false;
        _berserkerOrb -= amount;
        OnBerserkerOrbChanged?.Invoke();
        BerserkerGageUI.RefreshAll();
        return true;
    }
}
