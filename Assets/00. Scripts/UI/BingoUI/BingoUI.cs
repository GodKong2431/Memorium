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
    
    void Init()
    {
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
        BingoBoardManager.Instance.Init(_ctx);
    }
    
    void Start()
    {
        Init();
        
        int id = 3410001;
        
        foreach(var text in BingoButtons)
        {
            text.text = $"{InventoryManager.Instance.GetItemAmount(id).ToFloat()}";
            id++;
        }
    }

    void OnEnable()
    {
        InventoryManager.Instance.OnItemAmountChanged += OnSetLinkCounter;
    }

    void OnDisable()
    {
        InventoryManager.Instance.OnItemAmountChanged -= OnSetLinkCounter;
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
            BingoButtons[id-3410001].text = $"{InventoryManager.Instance.GetItemAmount(id).ToFloat()}";
    }
}
