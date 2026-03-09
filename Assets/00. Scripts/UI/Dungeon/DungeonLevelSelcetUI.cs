using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DungeonLevelSelectUI : MonoBehaviour
{
    public static DungeonLevelSelectUI Instance { get; private set; }

    [Header("텍스트")]
    public TextMeshProUGUI textTitle;
    public TextMeshProUGUI textCurrentLevel;

    [Header("버튼")]
    public Button btnPrevLevel;
    public Button btnNextLevel;
    public Button btnStartDungeon;
    public Button btnBack;

    private StageType currentDungeonType;
    private int currentLevel = 1;
    private int maxUnlockedLevel = 1;
    private int currentSelectedDungeonID;
    private BigDouble requiredTicketCount = new BigDouble(1);

    public event Action TicketStateChanged;

    public CurrencyType CurrentTicketType => GetTicketCurrencyType(currentDungeonType);

    public BigDouble RequiredTicketCount => requiredTicketCount;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        if (btnPrevLevel != null)
            btnPrevLevel.onClick.AddListener(OnClickPrev);

        if (btnNextLevel != null)
            btnNextLevel.onClick.AddListener(OnClickNext);

        if (btnStartDungeon != null)
            btnStartDungeon.onClick.AddListener(OnClickStart);

        if (btnBack != null)
            btnBack.onClick.AddListener(OnClickBackToList);
    }

    private void OnEnable()
    {
        GameEventManager.OnCurrencyChanged += OnCurrencyChanged;
        RefreshTicketState();
    }

    private void OnDisable()
    {
        GameEventManager.OnCurrencyChanged -= OnCurrencyChanged;
    }

    public void SetupDungeonType(StageType type)
    {
        currentDungeonType = type;

        if (textTitle != null)
            textTitle.text = $"{type}";

        maxUnlockedLevel = 3;
        currentLevel = 1;

        UpdateUI();
    }

    private void OnClickPrev()
    {
        if (currentLevel <= 1)
            return;

        currentLevel--;
        UpdateUI();
    }

    private void OnClickNext()
    {
        if (currentLevel >= maxUnlockedLevel)
            return;

        currentLevel++;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (textCurrentLevel != null)
            textCurrentLevel.text = $"Lv. {currentLevel}";

        if (btnPrevLevel != null)
            btnPrevLevel.interactable = currentLevel > 1;

        if (btnNextLevel != null)
            btnNextLevel.interactable = currentLevel < maxUnlockedLevel;
        RefreshTicketState();
    }

    private void OnClickStart()
    {
        if (currentSelectedDungeonID == 0)
            return;

        var currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currencyModule != null &&
            currencyModule.TrySpend(CurrentTicketType, requiredTicketCount))
        {
            Debug.Log($"[DungeonLevelSelectUI] 던전 ID:{currentSelectedDungeonID} / {CurrentTicketType} {requiredTicketCount}장 소모");

            DungeonManager.Instance.currentDungeonID = currentSelectedDungeonID;
            GlobalPopupManager.Instance.ClosePopup();
            StageManager.Instance.SetStageType(currentDungeonType, currentLevel);
            SceneController.Instance.LoadScene(SceneType.DungeonScene);
        }
        else
        {
            Debug.LogWarning($"[DungeonLevelSelectUI] {CurrentTicketType} 입장권이 부족합니다.");
        }
    }

    private void OnClickBackToList()
    {
        GlobalPopupManager.Instance.OpenPopupMode(PopupMode.DungeonList);
    }

    private int GetTargetDungeonID(StageType type, int level)
    {
        switch (type)
        {
            case StageType.GuardianTaxVault:
                return 6021000 + level;
            case StageType.HallOfTraining:
                return 6022000 + level;
            case StageType.CelestiAlchemyWorkshop:
                return 6023000 + level;
            case StageType.EidosTreasureVault:
                return 6024000 + level;
            default:
                return 0;
        }
    }

    private CurrencyType GetTicketCurrencyType(StageType type)
    {
        switch (type)
        {
            case StageType.GuardianTaxVault:
            case StageType.HallOfTraining:
            case StageType.CelestiAlchemyWorkshop:
            case StageType.EidosTreasureVault:
            default:
                return CurrencyType.DungeonTicket;
        }
    }

    private void RefreshTicketState()
    {
        if (currentDungeonType == StageType.None)
            return;

        currentSelectedDungeonID = GetTargetDungeonID(currentDungeonType, currentLevel);

        bool isDataValid = false;
        if (DataManager.Instance != null && DataManager.Instance.DungeonReqDict != null)
        {
            if (DataManager.Instance.DungeonReqDict.ContainsKey(currentSelectedDungeonID))
            {
                isDataValid = true;
            }
            else
            {
                Debug.LogWarning($"[DungeonLevelSelectUI] ID {currentSelectedDungeonID} 데이터가 없습니다.");
            }
        }

        requiredTicketCount = new BigDouble(1);
        var currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        bool hasEnough = currencyModule != null &&
                         currencyModule.HasEnough(CurrentTicketType, requiredTicketCount);

        if (btnStartDungeon != null)
            btnStartDungeon.interactable = hasEnough && isDataValid;

        TicketStateChanged?.Invoke();
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (currentDungeonType == StageType.None || type != CurrentTicketType)
            return;

        RefreshTicketState();
    }
}
