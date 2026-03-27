using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
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
    
    [SerializeField] List<BingoColumnSlot> SlotColumns = new List<BingoColumnSlot>();
    [SerializeField] private RectTransform retry;
    public BingoContext _ctx { get; private set; }

    public static event Action<int> OnClickBingoGachaButton;
    
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
        };
        BingoBoardManager.Instance.Init(_ctx);
    }
    
    void Start()
    {
        Init();
    }
    
    public void OnClickBingoGacha(int index)
    {
        OnClickBingoGachaButton?.Invoke(index);
    }
}
