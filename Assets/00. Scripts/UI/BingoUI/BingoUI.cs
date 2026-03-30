using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BingoUI : MonoBehaviour
{
    [SerializeField] private Button BingoBoardTransform;
    [SerializeField] private SerializedDictionary<int, List<GameObject>> BingoSlot = new SerializedDictionary<int, List<GameObject>>();  
    [SerializeField] private RectTransform ColSynergyTransform;
    [SerializeField] private RectTransform RowSynergyTransform;
    [SerializeField] private RectTransform DiaSynergyTransform;

    [SerializeField] private List<SynergyViewItem> SynergyViews;
    
    [SerializeField] private List<TextMeshProUGUI> BingoButtons;
    
    [SerializeField] List<BingoColumnSlot> SlotColumns = new List<BingoColumnSlot>();
    [SerializeField] private RectTransform retry;
    public BingoContext _ctx { get; private set; }
    public int linkItemId;
    public static event Action<int, int> OnClickBingoGachaButton;
    private bool isInitialized;
    
    void Init()
    {
        BingoBoardManager boardManager = FindFirstObjectByType<BingoBoardManager>();
        if (boardManager == null)
            return;

        _ctx = new BingoContext
        {
            retry = this.retry,
            BoardTransform = BingoBoardTransform,
            Slots = BingoSlot,
            ColumnSynergyTransform = ColSynergyTransform,
            RowLineSynergyTransform = RowSynergyTransform,
            DiaLineSynergyTransform = DiaSynergyTransform,
            SynergyViewObjects = SynergyViews,
            Columns = new List<BingoColumnSlot>(SlotColumns),
            BingoButtons = BingoButtons,
        };
        
        boardManager.Init(_ctx);
    }
    
    public void EnsureInitialized()
    {
        if (isInitialized)
            return;

        Init();

        BingoBoardManager boardManager = FindFirstObjectByType<BingoBoardManager>();
        if (boardManager != null && boardManager.LoadBingo)
            isInitialized = true;
    }
    
    void Start()
    {
        EnsureInitialized();
        RefreshLinkCounters();
    }

    void OnEnable()
    {
        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager != null)
            inventoryManager.OnItemAmountChanged += OnSetLinkCounter;
    }

    void OnDisable()
    {
        BingoBoardManager boardManager = FindFirstObjectByType<BingoBoardManager>();
        if (boardManager != null)
            boardManager.ResetForBingoUiDisable();

        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager != null)
        {
            inventoryManager.OnItemAmountChanged -= OnSetLinkCounter;
        }
    }

    public void OnIDSet(int index)
    {
        linkItemId = index;
    }
    
    public void OnClickBingoGacha(int index)
    {
        OnClickBingoGachaButton?.Invoke(index, linkItemId);
    }
    public void OnSetLinkCounter(InventoryItemContext item, BigDouble amount)
    {
        int startId = 3410001;
        int endId = startId + BingoButtons.Count - 1;

        if (item.ItemId < startId || item.ItemId > endId)
            return;

        OnSetLinkCounter(item.ItemId);
    }
    
    public void OnSetLinkCounter(int id)
    {
        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null)
            return;

        int index = id - 3410001;
        if (index < 0 || index >= BingoButtons.Count || BingoButtons[index] == null)
            return;

        BingoButtons[index].text = $"{inventoryManager.GetItemAmount(id).ToFloat()}";
    }

    private void RefreshLinkCounters()
    {
        InventoryManager inventoryManager = FindFirstObjectByType<InventoryManager>();
        if (inventoryManager == null || !inventoryManager.DataLoad)
            return;

        int id = 3410001;
        foreach (var text in BingoButtons)
        {
            if (text != null)
                text.text = $"{inventoryManager.GetItemAmount(id).ToFloat()}";
            id++;
        }
    }
}
