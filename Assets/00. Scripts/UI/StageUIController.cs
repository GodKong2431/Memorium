using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageUIController : MonoBehaviour
{
    [Header("팝업 스테이지 레벨")]
    [SerializeField] private TextMeshProUGUI textPopupStageLevel;

    [Header("MapInfo 스테이지")]
    [SerializeField] private TextMeshProUGUI textMapInfoStageName;
    [SerializeField] private TextMeshProUGUI textMapInfoStageLevel;

    [Header("진행도 UI")]
    [SerializeField] private TextMeshProUGUI textProgress;
    [SerializeField] private Slider sliderStageProgressBar;

    [Header("보스 소환")]
    [SerializeField] private Button btnSummonBoss;
    [SerializeField] private Image imageSummonBossButton;
    [SerializeField] private Image imageSummonBossIcon;

    [Header("보스 소환 색상")]
    [SerializeField] private Color colorSummonBossEnabled = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color colorSummonBossDisabled = new Color(0.49411765f, 0.49411765f, 0.49411765f, 1f);

    private StageUIView stageUI;

    // 인스펙터에 직렬화된 참조로 UI 뷰 래퍼를 초기화한다.
    private void Awake()
    {
        stageUI = new StageUIView(
            textPopupStageLevel,
            textMapInfoStageName,
            textMapInfoStageLevel,
            textProgress,
            sliderStageProgressBar,
            btnSummonBoss,
            imageSummonBossButton,
            imageSummonBossIcon,
            colorSummonBossEnabled,
            colorSummonBossDisabled
        );
    }

    // 이벤트를 구독하고 현재 스테이지 상태를 즉시 반영한다.
    private void OnEnable()
    {
        GameEventManager.OnStageChanged += UpdateStageChanged;
        GameEventManager.OnStageProgressChanged += UpdateStageProgress;

        stageUI.BindSummonButton(OnClickSummonBoss);
        stageUI.SetSummonInteractable(false);
        RefreshFromCurrentState();
    }

    // 비활성화 시 이벤트를 해제해 중복 호출을 방지한다.
    private void OnDisable()
    {
        GameEventManager.OnStageChanged -= UpdateStageChanged;
        GameEventManager.OnStageProgressChanged -= UpdateStageProgress;

        stageUI.UnbindSummonButton(OnClickSummonBoss);
    }

    // 스테이지 변경 이벤트를 받아 이름/레벨을 갱신한다.
    private void UpdateStageChanged(int chapter, int stage)
    {
        stageUI.SetStageLevel(chapter, stage);
        stageUI.SetStageName(ResolveSceneStageName());
    }

    // 진행도 변경 이벤트를 받아 텍스트/슬라이더를 갱신한다.
    private void UpdateStageProgress(int currentKill, int maxKill)
    {
        stageUI.SetStageProgress(currentKill, maxKill);
    }

    // 보스 소환 버튼 클릭 시 버튼을 잠그고 소환 이벤트를 전달한다.
    private void OnClickSummonBoss()
    {
        stageUI.SetSummonInteractable(false);
        GameEventManager.OnSummonBossClicked?.Invoke();
    }

    // 패널 재진입 시 현재 스테이지 이름/레벨/진행도를 다시 그린다.
    private void RefreshFromCurrentState()
    {
        if (StageManager.Instance == null)
            return;

        int chapter = StageManager.Instance.curFloor;
        int stage = ResolveSceneStageNumber();
        if (chapter > 0 && stage > 0)
            stageUI.SetStageLevel(chapter, stage);

        stageUI.SetStageName(ResolveSceneStageName());
        stageUI.SetStageProgress(StageManager.Instance.curMonsterKillCount, StageManager.Instance.maxMonsterKillCount);
    }

    // 현재 인덱스를 씬 표기용 스테이지 번호로 변환한다.
    private static int ResolveSceneStageNumber()
    {
        if (!TryResolveCurrentStageData(out StageManageTable stageData))
            return 0;

        return stageData.sceneNumber;
    }

    // 현재 스테이지 이름(지역명)을 조회한다.
    private static string ResolveSceneStageName()
    {
        if (!TryResolveCurrentStageData(out StageManageTable stageData))
            return string.Empty;

        return stageData.stageName;
    }

    // 현재 StageManager/DataManager 상태를 기준으로 스테이지 데이터를 조회한다.
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
}
