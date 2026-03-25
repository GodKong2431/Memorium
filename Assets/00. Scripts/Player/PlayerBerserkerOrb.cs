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


    private int _currentBerserkerOrb;

    public int CurrentBerserkerOrb => _currentBerserkerOrb;

    public event Action OnBerserkerOrbChanged;
    
    public event Action OnBerserkerOrbFull;
    
    public bool isAuto;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void Init(BerserkmodeManageTable table)
    {
        if (table == null) return;
        MaxBerserkerOrb = table.berserkCounter;
        Debug.Log(MaxBerserkerOrb);
        NormalBerserkerOrb = table.normalDropQty;
        Debug.Log(NormalBerserkerOrb);
        BossBerserkerOrb = table.bossDropQty;
        Debug.Log(BossBerserkerOrb);
    }

    private void OnEnable()
    {
        BerserkerOrb.OnBerserkerOrbEarned -= AddBerserkerOrb;
        BerserkerOrb.OnBerserkerOrbEarned += AddBerserkerOrb;
        // 씬 왕복/재활성화 시 중복 구독 방지 후 재구독
        //BerserkerOrb.OnBerserkerOrbEarned -= AddBerserkerOrb;
        //EnemyKillRewardDispatcher.OnBerserkerOrbEarned += AddBerserkerOrb;
    }

    private void OnDisable()
    {
        //EnemyKillRewardDispatcher.OnBerserkerOrbEarned -= AddBerserkerOrb;
        BerserkerOrb.OnBerserkerOrbEarned -= AddBerserkerOrb;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void AddBerserkerOrb(int amount)
    {
        if (amount <= 0) return;
        _currentBerserkerOrb = Mathf.Min(MaxBerserkerOrb, _currentBerserkerOrb + amount);
        OnBerserkerOrbChanged?.Invoke();
        BerserkerGageUI.RefreshAll();
        if (isAuto && MaxBerserkerOrb == _currentBerserkerOrb)
        {
            OnBerserkerOrbFull?.Invoke();
        }
    }

    /// <summary>버서커 모드 발동 시 오브 소모. 보유량이 부족하면 false.</summary>
    public bool TryConsumeBerserkerOrbs(int amount)
    {
        if (amount <= 0 || _currentBerserkerOrb < amount) return false;
        _currentBerserkerOrb -= amount;
        OnBerserkerOrbChanged?.Invoke();
        BerserkerGageUI.RefreshAll();
        return true;
    }
}
