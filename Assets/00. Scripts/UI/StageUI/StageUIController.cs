using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class StageUIController : UIControllerBase
{
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

    private StageUIView stageView;

    protected override void Initialize()
    {
        ResolveStageTextReferences();

        stageView = new StageUIView(
            textPopupStageLevel,
            textMapInfoStageName,
            textMapInfoStageLevel,
            textMapInfoFloor,
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
        ResolveStageTextReferences();

        if (StageManager.Instance == null)
        {
            stageView.Render("-", string.Empty, "-", true, "-", false, 0, 0);
            stageView.SetSummonInteractable(false);
            stageView.SetBossMode(false);
            return;
        }

        bool isDungeonScene = IsDungeonScene();
        string stageLevelText = isDungeonScene
            ? FormatDungeonLevel(StageManager.Instance.curStage)
            : FormatStageLevel(StageManager.Instance.curFloor, ResolveSceneStageNumber());
        string stageName = ResolveSceneStageName();
        int currentKill = StageManager.Instance.curMonsterKillCount;
        int maxKill = StageManager.Instance.maxMonsterKillCount;

        stageView.Render(
            stageLevelText,
            stageName,
            stageLevelText,
            !isDungeonScene,
            stageLevelText,
            isDungeonScene,
            currentKill,
            maxKill);
        stageView.SetSummonInteractable(CalculateSummonInteractable(currentKill, maxKill));
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

        if (StageManager.Instance.onFailedStage && StageManager.Instance.CurrentStageType == StageType.NormalStage)
            return true;

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

    private void ResolveStageTextReferences()
    {
        Transform mapInfoRoot = ResolveMapInfoRoot();
        textMapInfoStageLevel = ResolveTextReference(textMapInfoStageLevel, mapInfoRoot, "(Text)Level");
        textMapInfoFloor = ResolveTextReference(textMapInfoFloor, mapInfoRoot, "(Text)Floor");

        if (stageView != null)
            stageView.UpdateStageTextTargets(textPopupStageLevel, textMapInfoStageName, textMapInfoStageLevel, textMapInfoFloor);
    }

    private Transform ResolveMapInfoRoot()
    {
        if (IsMatchingText(textMapInfoStageLevel, "(Text)Level") && textMapInfoStageLevel.transform.parent != null)
            return textMapInfoStageLevel.transform.parent;

        if (IsMatchingText(textMapInfoFloor, "(Text)Floor") && textMapInfoFloor.transform.parent != null)
            return textMapInfoFloor.transform.parent;

        if (textMapInfoStageName != null && textMapInfoStageName.transform.parent != null)
            return textMapInfoStageName.transform.parent;

        Transform topPanel = FindTransformByName(transform.root, "TopPanel");
        if (topPanel == null)
            return null;

        TextMeshProUGUI stageNameText = FindTextByName(topPanel, "(Text)Name");
        if (stageNameText != null && stageNameText.transform.parent != null)
            return stageNameText.transform.parent;

        return topPanel;
    }

    private static TextMeshProUGUI ResolveTextReference(TextMeshProUGUI current, Transform searchRoot, string objectName)
    {
        if (IsMatchingText(current, objectName))
            return current;

        TextMeshProUGUI resolved = FindTextByName(searchRoot, objectName);
        if (resolved != null)
            return resolved;

        TextMeshProUGUI[] texts = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < texts.Length; i++)
        {
            if (IsMatchingText(texts[i], objectName))
                return texts[i];
        }

        return current;
    }

    private static bool IsMatchingText(TextMeshProUGUI target, string objectName)
    {
        return target != null && target.gameObject.name == objectName;
    }

    private static TextMeshProUGUI FindTextByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        TextMeshProUGUI[] texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (IsMatchingText(texts[i], objectName))
                return texts[i];
        }

        return null;
    }

    private static Transform FindTransformByName(Transform root, string objectName)
    {
        if (root == null)
            return null;

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
                return transforms[i];
        }

        return null;
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
}
