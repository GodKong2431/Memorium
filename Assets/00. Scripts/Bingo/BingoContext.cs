using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BingoContext
{
    public Button BoardTransform {get; set;}
    public SerializedDictionary<int, List<GameObject>> Slots {get; set;}
    public RectTransform ColumnSynergyTransform {get; set;}
    public RectTransform RowLineSynergyTransform {get; set;}
    public RectTransform DiaLineSynergyTransform {get; set;}
    public RectTransform retry {get; set;}
    public List<SynergyViewItem> SynergyViewObjects {get; set;}
    public List<BingoColumnSlot> Columns {get; set;}
    
    public BingoContext()
    {
        
    }
}
