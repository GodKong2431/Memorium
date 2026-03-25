using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BingoUI : MonoBehaviour
{
    [SerializeField] private RectTransform BingoBoardTransform;

    [SerializeField] private SerializedDictionary<int, List<GameObject>> BingoSlot = new SerializedDictionary<int, List<GameObject>>();  

    [SerializeField] private RectTransform ColSynergyTransform;
    [SerializeField] private RectTransform RowSynergyTransform;

    [SerializeField] private List<GameObject> Synergys;
    
    [SerializeField] List<BingoColumnSlot> SlotColumns = new List<BingoColumnSlot>();
    public BingoContext _ctx { get; private set; }

    public static event Action<int> OnClickBingoGachaButton;
    
    void Init()
    {
        _ctx = new BingoContext
        {
            Slots = BingoSlot,
            ColumnSynergyTransform = ColSynergyTransform,
            RowLineSynergyTransform = RowSynergyTransform,
            SynergyObjects = Synergys,
            Columns = SlotColumns,
        };
        BingoBoard.Instance.Init(_ctx);
    }
    
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void OnClickBingoGacha(int index)
    {
        OnClickBingoGachaButton?.Invoke(index);
    }
}
