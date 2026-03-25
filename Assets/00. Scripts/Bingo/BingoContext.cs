using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

public class BingoContext
{
    public RectTransform BoardTransform {get; set;}
    public SerializedDictionary<int, List<GameObject>> Slots {get; set;}
    public RectTransform ColumnSynergyTransform {get; set;}
    public RectTransform RowLineSynergyTransform {get; set;}
    public List<GameObject> SynergyObjects {get; set;}
    public List<BingoColumnSlot> Columns {get; set;}
    
    public BingoContext()
    {
        
    }
}
