using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageUIController : UIControllerBase
{
    [Header("Sort")]
    [SerializeField] private int backOrder = -20;

    [Header("Popup Stage Level")]
    [SerializeField] private TextMeshProUGUI textPopupStageLevel;

    [Header("MapInfo Stage")]
    [SerializeField] private TextMeshProUGUI textMapInfoStageName;
    [SerializeField] private TextMeshProUGUI textMapInfoStageLevel;
    [SerializeField] private TextMeshProUGUI textMapInfoFloor;

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

    private Canvas backCanvas;
    private GraphicRaycaster backRaycaster;

    protected override void Initialize()
    {
        ApplyBackSort();
        SetSummonInteractable(false);
        WarnBossUi();
    }

    protected override void OnEnable()
    {
        ApplyBackSort();
        base.OnEnable();
    }

    protected override void Subscribe()
    {
        GameEventManager.OnStageChanged += OnStageChanged;
        GameEventManager.OnStageProgressChanged += OnStageProgressChanged;
        BindSummonButton();
    }

    protected override void Unsubscribe()
    {
        GameEventManager.OnStageChanged -= OnStageChanged;
        GameEventManager.OnStageProgressChanged -= OnStageProgressChanged;
        UnbindSummonButton();
    }

    protected override void RefreshView()
    {
        if (StageManager.Instance == null)
        {
            RenderStageInfo("-", string.Empty, "-", true, "-", false, 0, 0);
            SetSummonInteractable(false);
            SetBossMode(false);
            return;
        }

        bool isDungeonScene = IsDungeonScene();
        string stageLevelText = isDungeonScene
            ? FormatDungeonLevel(StageManager.Instance.curStage)
            : FormatStageLevel(StageManager.Instance.curFloor, ResolveSceneStageNumber());
        string stageName = ResolveSceneStageName();
        int currentKill = StageManager.Instance.curMonsterKillCount;
        int maxKill = StageManager.Instance.maxMonsterKillCount;

        RenderStageInfo(
            stageLevelText,
            stageName,
            stageLevelText,
            !isDungeonScene,
            stageLevelText,
            isDungeonScene,
            currentKill,
            maxKill);
        SetSummonInteractable(CalculateSummonInteractable(currentKill, maxKill));
        RefreshBossUi();
    }

    private void Update()
    {
        RefreshBossUi();
    }

    private void OnStageChanged(int chapter, int stage)
    {
        RefreshView();
    }

    private void OnStageProgressChanged(int currentKill, int maxKill)
    {
        SetStageProgress(currentKill, maxKill);
        SetSummonInteractable(CalculateSummonInteractable(currentKill, maxKill));
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

        if (StageManager.Instance.onFailedStage && StageManager.Instance.CurrentStageType == StageType.NormalStage)
            return true;

        return !StageManager.Instance.HasPendingBossSpawnRequest;
    }

    private void SyncSummonButtonState()
    {
        if (StageManager.Instance == null)
        {
            SetSummonInteractable(false);
            return;
        }

        SetSummonInteractable(
            CalculateSummonInteractable(
                StageManager.Instance.curMonsterKillCount,
                StageManager.Instance.maxMonsterKillCount));
    }

    private void RefreshBossUi()
    {
        if (StageManager.Instance == null)
        {
            SetBossMode(false);
            return;
        }

        bool isBossStage = StageManager.Instance.IsBossStage;
        SetBossMode(isBossStage);

        if (!isBossStage)
            return;

        SetBoss(
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

    private static bool IsDungeonScene()
    {
        if (SceneManager.GetActiveScene().name == SceneType.DungeonScene.ToString())
            return true;

        return StageManager.Instance != null && StageManager.Instance.IsDungeonInProgress;
    }

    private static string FormatStageLevel(int chapter, int stageNumber)
    {
        return chapter > 0 && stageNumber > 0 ? $"{chapter}-{stageNumber}" : "-";
    }

    private static string FormatDungeonLevel(int dungeonLevel)
    {
        return dungeonLevel > 0 ? $"{dungeonLevel}\uB2E8\uACC4" : "-";
    }

    private void BindSummonButton()
    {
        if (btnSummonBoss == null)
            return;

        btnSummonBoss.onClick.RemoveListener(OnClickSummonBoss);
        btnSummonBoss.onClick.AddListener(OnClickSummonBoss);
    }

    private void UnbindSummonButton()
    {
        if (btnSummonBoss != null)
            btnSummonBoss.onClick.RemoveListener(OnClickSummonBoss);
    }

    private void RenderStageInfo(
        string popupStageLevel,
        string stageName,
        string mapInfoLevel,
        bool showMapInfoLevel,
        string mapInfoFloor,
        bool showMapInfoFloor,
        int currentKill,
        int maxKill)
    {
        SetText(textPopupStageLevel, popupStageLevel);
        SetText(textMapInfoStageName, stageName);
        SetText(textMapInfoStageLevel, mapInfoLevel);
        SetText(textMapInfoFloor, mapInfoFloor);
        SetTextVisible(textMapInfoStageLevel, showMapInfoLevel);
        SetTextVisible(textMapInfoFloor, showMapInfoFloor);
        SetStageProgress(currentKill, maxKill);
    }

    private void SetStageProgress(int currentKill, int maxKill)
    {
        int clampedMax = Mathf.Max(0, maxKill);
        int clampedCurrent = Mathf.Clamp(currentKill, 0, clampedMax);
        int progressPercent = clampedMax > 0
            ? Mathf.RoundToInt((clampedCurrent / (float)clampedMax) * 100f)
            : 0;

        if (textProgress != null)
            textProgress.text = $"{progressPercent}%";

        if (sliderStageProgressBar != null)
        {
            sliderStageProgressBar.minValue = 0f;
            sliderStageProgressBar.maxValue = clampedMax > 0 ? clampedMax : 1f;
            sliderStageProgressBar.SetValueWithoutNotify(clampedCurrent);
        }
    }

    private void SetBossMode(bool active)
    {
        bool showBoss = active && bossPanel != null;

        if (textProgress != null)
            textProgress.gameObject.SetActive(!showBoss);

        if (sliderStageProgressBar != null)
            sliderStageProgressBar.gameObject.SetActive(!showBoss);

        if (bossPanel != null)
            bossPanel.SetActive(showBoss);
    }

    private void SetBoss(float currentHp, float maxHp, float time)
    {
        if (bossTimeText != null)
            bossTimeText.text = time <= 0f ? "0.000" : time.ToString("0.000");

        if (bossHpText != null)
            bossHpText.text = FormatBossHp(currentHp, maxHp);

        if (bossHpSlider != null)
        {
            float clampedMax = Mathf.Max(1f, maxHp);
            bossHpSlider.minValue = 0f;
            bossHpSlider.maxValue = clampedMax;
            bossHpSlider.SetValueWithoutNotify(Mathf.Clamp(currentHp, 0f, clampedMax));
        }
    }

    private void SetSummonInteractable(bool interactable)
    {
        if (btnSummonBoss != null)
            btnSummonBoss.interactable = interactable;

        Color color = interactable ? colorSummonBossEnabled : colorSummonBossDisabled;

        if (imageSummonBossButton != null)
            imageSummonBossButton.color = color;

        if (imageSummonBossIcon != null)
            imageSummonBossIcon.color = color;
    }

    private static string FormatBossHp(float currentHp, float maxHp)
    {
        if (maxHp <= 0f)
            return "0%";

        float percent = Mathf.Clamp01(currentHp / maxHp) * 100f;
        return $"{Mathf.RoundToInt(percent)}%";
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null)
            target.text = string.IsNullOrEmpty(value) ? "-" : value;
    }

    private static void SetTextVisible(TextMeshProUGUI target, bool visible)
    {
        if (target != null)
            target.gameObject.SetActive(visible);
    }

    private void ApplyBackSort()
    {
        Transform parent = transform.parent;
        Canvas rootCanvas = parent != null ? parent.GetComponentInParent<Canvas>() : null;
        if (rootCanvas == null)
            return;

        if (backCanvas == null)
            backCanvas = GetComponent<Canvas>();
        if (backCanvas == null)
            backCanvas = gameObject.AddComponent<Canvas>();

        backCanvas.overrideSorting = true;
        backCanvas.renderMode = rootCanvas.renderMode;
        backCanvas.sortingLayerID = rootCanvas.sortingLayerID;
        backCanvas.sortingOrder = backOrder;
        backCanvas.worldCamera = rootCanvas.worldCamera;
        backCanvas.planeDistance = rootCanvas.planeDistance;

        if (backRaycaster == null)
            backRaycaster = GetComponent<GraphicRaycaster>();
        if (backRaycaster == null)
            backRaycaster = gameObject.AddComponent<GraphicRaycaster>();
    }
}
