using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageUIController : UIControllerBase
{
    [Header("Popup Stage Level")]
    [SerializeField] private TextMeshProUGUI textPopupStageLevel;

    [Header("MapInfo Stage")]
    [SerializeField] private TextMeshProUGUI textMapInfoStageName;
    [SerializeField] private TextMeshProUGUI textMapInfoStageLevel;

    [Header("Stage Progress UI")]
    [SerializeField] private TextMeshProUGUI textProgress;
    [SerializeField] private Slider sliderStageProgressBar;

    [Header("Boss HP UI")]
    [FormerlySerializedAs("objBossHpPanelRoot")]
    [SerializeField] private GameObject bossPanel;
    [FormerlySerializedAs("textBossTimer")]
    [SerializeField] private TextMeshProUGUI bossTimeText;
    [FormerlySerializedAs("textBossHp")]
    [SerializeField] private TextMeshProUGUI bossHpText;
    [FormerlySerializedAs("sliderBossHp")]
    [SerializeField] private Slider bossHpSlider;

    [Header("Boss Summon")]
    [SerializeField] private Button btnSummonBoss;
    [SerializeField] private Image imageSummonBossButton;
    [SerializeField] private Image imageSummonBossIcon;

    [Header("Boss Summon Colors")]
    [SerializeField] private Color colorSummonBossEnabled = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color colorSummonBossDisabled = new Color(0.49411765f, 0.49411765f, 0.49411765f, 1f);

    private StageUIView stageView;

    protected override void Initialize()
    {
        stageView = new StageUIView(
            textPopupStageLevel,
            textMapInfoStageName,
            textMapInfoStageLevel,
            textProgress,
            sliderStageProgressBar,
            bossPanel,
            bossTimeText,
            bossHpText,
            bossHpSlider,
            btnSummonBoss,
            imageSummonBossButton,
            imageSummonBossIcon,
            colorSummonBossEnabled,
            colorSummonBossDisabled
        );

        WarnBossUi();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnStageChanged += OnStageChanged;
        GameEventManager.OnStageProgressChanged += OnStageProgressChanged;
        stageView.BindSummonButton(OnClickSummonBoss);
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnStageChanged -= OnStageChanged;
        GameEventManager.OnStageProgressChanged -= OnStageProgressChanged;
        stageView.UnbindSummonButton(OnClickSummonBoss);
    }

    protected override void RefreshView()
    {
        if (StageManager.Instance == null)
        {
            stageView.Render(0, 0, string.Empty, 0, 0);
            stageView.SetSummonInteractable(false);
            stageView.SetBossMode(false);
            return;
        }

        int chapter = StageManager.Instance.curFloor;
        int stageNumber = ResolveSceneStageNumber();
        string stageName = ResolveSceneStageName();
        int currentKill = StageManager.Instance.curMonsterKillCount;
        int maxKill = StageManager.Instance.maxMonsterKillCount;

        stageView.Render(chapter, stageNumber, stageName, currentKill, maxKill);
        stageView.SetSummonInteractable(CalculateSummonInteractable(currentKill, maxKill));
        RefreshBossUi();
    }

    private void Update()
    {
        RefreshBossUi();
    }

    private void OnStageChanged(int chapter, int stage)
    {
        stageView.SetStageLevel(chapter, stage);
        stageView.SetStageName(ResolveSceneStageName());
        RefreshBossUi();
    }

    private void OnStageProgressChanged(int currentKill, int maxKill)
    {
        stageView.SetStageProgress(currentKill, maxKill);
        stageView.SetSummonInteractable(CalculateSummonInteractable(currentKill, maxKill));
        RefreshBossUi();
    }

    private void OnClickSummonBoss()
    {
        GameEventManager.OnSummonBossClicked?.Invoke();
        SyncSummonButtonState();
    }

    private static int ResolveSceneStageNumber()
    {
        return TryResolveCurrentStageData(out StageManageTable stageData) ? stageData.sceneNumber : 0;
    }

    private static string ResolveSceneStageName()
    {
        return TryResolveCurrentStageData(out StageManageTable stageData) ? stageData.stageName : string.Empty;
    }

    private static bool TryResolveCurrentStageData(out StageManageTable stageData)
    {
        stageData = null;

        if (StageManager.Instance == null)
            return false;
        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.StageManageDict == null)
            return false;
        if (StageManager.Instance.stageKeyList == null || StageManager.Instance.stageKeyList.Count == 0)
            return false;

        int index = Mathf.Clamp(StageManager.Instance.curStage - 1, 0, StageManager.Instance.stageKeyList.Count - 1);
        int stageKey = StageManager.Instance.stageKeyList[index];
        return DataManager.Instance.StageManageDict.TryGetValue(stageKey, out stageData);
    }

    private static bool CalculateSummonInteractable(int currentKill, int maxKill)
    {
        if (StageManager.Instance == null)
            return false;

        int clampedMax = Mathf.Max(0, maxKill);
        int clampedCurrent = Mathf.Clamp(currentKill, 0, clampedMax);

        if (clampedMax <= 0 || clampedCurrent < clampedMax)
            return false;

        if (StageManager.Instance.onBossStage)
            return false;

        return !StageManager.Instance.HasPendingBossSpawnRequest;
    }

    private void SyncSummonButtonState()
    {
        if (StageManager.Instance == null)
        {
            stageView.SetSummonInteractable(false);
            return;
        }

        stageView.SetSummonInteractable(
            CalculateSummonInteractable(
                StageManager.Instance.curMonsterKillCount,
                StageManager.Instance.maxMonsterKillCount));
    }

    private void RefreshBossUi()
    {
        if (stageView == null)
            return;

        if (StageManager.Instance == null)
        {
            stageView.SetBossMode(false);
            return;
        }

        bool isBossStage = StageManager.Instance.IsBossStage;
        stageView.SetBossMode(isBossStage);

        if (!isBossStage)
            return;

        stageView.SetBoss(
            StageManager.Instance.BossHp,
            StageManager.Instance.BossMaxHp,
            StageManager.Instance.BossTime);
    }

    private void WarnBossUi()
    {
        if (bossPanel != null
            && bossTimeText != null
            && bossHpText != null
            && bossHpSlider != null)
            return;

        Debug.LogWarning("[StageUIController] Boss HP UI references are missing. Assign (Panel)BossHp and its children in the inspector.");
    }
}
