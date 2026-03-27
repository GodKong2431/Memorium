using System;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class PlayerBerserkerOrb : MonoBehaviour
{
    public static PlayerBerserkerOrb Instance { get; private set; }

    public static int MaxBerserkerOrb { get; private set; }
    public static int NormalBerserkerOrb { get; private set; }
    public static int BossBerserkerOrb { get; private set; }

    private int currentBerserkerOrb;

    public int CurrentBerserkerOrb => currentBerserkerOrb;

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
        ApplyAutoOption(allowAutoTrigger: false);
    }

    public void Init(BerserkmodeManageTable table)
    {
        if (table == null)
            return;

        MaxBerserkerOrb = table.berserkCounter;
        Debug.Log(MaxBerserkerOrb);

        NormalBerserkerOrb = table.normalDropQty;
        Debug.Log(NormalBerserkerOrb);

        BossBerserkerOrb = table.bossDropQty;
        Debug.Log(BossBerserkerOrb);

        ApplyAutoOption(allowAutoTrigger: true);
    }

    private void OnEnable()
    {
        BerserkerOrb.OnBerserkerOrbEarned -= AddBerserkerOrb;
        BerserkerOrb.OnBerserkerOrbEarned += AddBerserkerOrb;

        GameOptionSettings.UseManualBerserkerModeChanged -= HandleManualModeChanged;
        GameOptionSettings.UseManualBerserkerModeChanged += HandleManualModeChanged;

        ApplyAutoOption(allowAutoTrigger: true);
    }

    private void OnDisable()
    {
        BerserkerOrb.OnBerserkerOrbEarned -= AddBerserkerOrb;
        GameOptionSettings.UseManualBerserkerModeChanged -= HandleManualModeChanged;
    }

    private void OnDestroy()
    {
        GameOptionSettings.UseManualBerserkerModeChanged -= HandleManualModeChanged;

        if (Instance == this)
            Instance = null;
    }

    public void AddBerserkerOrb(int amount)
    {
        if (amount <= 0)
            return;

        currentBerserkerOrb = Mathf.Min(MaxBerserkerOrb, currentBerserkerOrb + amount);
        OnBerserkerOrbChanged?.Invoke();
        BerserkerGageUI.RefreshAll();

        if (isAuto && MaxBerserkerOrb == currentBerserkerOrb)
            OnBerserkerOrbFull?.Invoke();
    }

    public bool TryConsumeBerserkerOrbs(int amount)
    {
        if (amount <= 0 || currentBerserkerOrb < amount)
            return false;

        currentBerserkerOrb -= amount;
        OnBerserkerOrbChanged?.Invoke();
        BerserkerGageUI.RefreshAll();
        return true;
    }

    private void HandleManualModeChanged(bool _)
    {
        ApplyAutoOption(allowAutoTrigger: true);
    }

    private void ApplyAutoOption(bool allowAutoTrigger)
    {
        isAuto = !GameOptionSettings.UseManualBerserkerMode;
        BerserkerGageUI.RefreshAll();

        if (!allowAutoTrigger || !isAuto || MaxBerserkerOrb <= 0 || currentBerserkerOrb < MaxBerserkerOrb)
            return;

        OnBerserkerOrbFull?.Invoke();
    }
}
