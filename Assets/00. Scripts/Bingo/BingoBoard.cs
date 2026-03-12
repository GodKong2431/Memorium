using AYellowpaper.SerializedCollections;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;   

public class BingoBoard : Singleton<BingoBoard>
{
    [SerializeField] public SerializedDictionary<BingoColumnSlot, List<BingoSlot>> SlotList = new SerializedDictionary<BingoColumnSlot, List<BingoSlot>>();
    [SerializeField] List<BingoColumnSlot> SlotColumns = new List<BingoColumnSlot>();
    [SerializeField] BingoBoardSO bingoBoardSO;
            
    [SerializeField] SerializedDictionary<CellRarity, List<BingoSlot>> SlotGradeList = new SerializedDictionary<CellRarity, List<BingoSlot>>();
    
    [SerializeField] SerializedDictionary<int, BingoSynergy> RowSynergy = new SerializedDictionary<int, BingoSynergy>();
    [SerializeField] SerializedDictionary<int, BingoSynergy> ColSynergy = new SerializedDictionary<int, BingoSynergy>();
    [SerializeField] BingoSynergy DiagSynergy;
    
    [SerializeField] public List<BingoSynergy> Synergies = new List<BingoSynergy>();
    
    [SerializeField] private Button slotTest;
    [SerializeField] private CellRarity test1;
    [SerializeField] private RarityType test2;
    [SerializeField] private StatType test3;
    [SerializeField] private SynergyManager synergyMgr;
    [SerializeField] private Transform transform1;
    [SerializeField] private Transform transform2;
    [SerializeField] private TMP_Dropdown dropdown;
    [SerializeField] public BingoItemManager bingoItemManager;
    
    [SerializeField] public TMP_Dropdown RarityDropDown;
    [SerializeField] public TMP_Dropdown StatDropDown;
    
    event Action testEvent;
    bool eventTriggered;
    
    public ItemBase againItem; 
    public bool againGacha;
    
    bool isAgain;
    
    public Button bingoButton;
    
    public bool LoadBingo;
    
    public int BingoRange
    {
        get => SlotList.Keys.Count();
    }

    void Start()
    {
        
        // DropdownSet<CellRarity>(ref dropdown, value => test1 = value);
        // DropdownSet<RarityType>(ref RarityDropDown, value => synergyMgr.rarity = value);
        // DropdownSet<SynergyStat>(ref StatDropDown, value => synergyMgr.stat = value);
        
        // bingoButton.onClick.AddListener(() => OnClick(bingoItemManager.itemBase));
        // slotTest.onClick.RemoveAllListeners();
        // slotTest.onClick.AddListener(()=> Testpe());
        
        foreach (var slotColumn in SlotColumns)
        {
            SlotList.Add(slotColumn, new List<BingoSlot>());
        }
        
        foreach(CellRarity grade in Enum.GetValues(typeof(CellRarity)))
        {
            SlotGradeList.Add(grade, new List<BingoSlot>());
        }
        
        if (transform1 == null && transform2 == null)
        {
            return;
        }
        
        foreach (var synergy in bingoBoardSO.bingoSynergy)
        {
            foreach (var item in synergy.Value)
            {
                switch (synergy.Key)
                {
                    case SynergyDirection.Row:
                        item.Value.Init(synergy.Key,item.Key);
                        var synergyRowItem = Instantiate(item.Value , transform1);
                        RowSynergy.Add(item.Key,synergyRowItem);
                        Synergies.Add(synergyRowItem);
                        break;
                    case SynergyDirection.Column:
                        item.Value.Init(synergy.Key,item.Key);
                        var synergyColItem = Instantiate(item.Value , transform2);
                        ColSynergy.Add(item.Key,synergyColItem);
                        Synergies.Add(synergyColItem);
                        break;
                    case SynergyDirection.Diagonal:
                        item.Value.Init(synergy.Key,item.Key);
                        DiagSynergy = Instantiate(item.Value , transform2);
                        Synergies.Add(DiagSynergy);
                        break;
                }
            }
        }
        
        for (int col = 0; col < bingoBoardSO.bingoSlots.Count; col++)
        {
            var slotColumn = bingoBoardSO.bingoSlots[col];
            
            for (int row = 0; row < slotColumn.Count; row++)
            {
                BingoSlot slotItem = Instantiate(slotColumn[row] , SlotColumns[col].transform);
                
                slotItem.Init(col,row);
                slotItem.UpdateUnlock += RowSynergy[row].Check;
                slotItem.UpdateUnlock += ColSynergy[col].Check;
                
                if (col == row)
                {
                    slotItem.UpdateUnlock += DiagSynergy.Check;
                }
                
                SlotList[SlotColumns[col]].Add(slotItem);
                SlotGradeList[slotColumn[row].bingoGrade].Add(slotItem);
            }
        }
    }
    
    public void DropdownSet<T>(ref TMP_Dropdown dropdown, Action<T> setValue) where T : Enum
    {
        dropdown.ClearOptions();
        
        var EnumType = Enum.GetNames(typeof(T)).ToList();
        dropdown.AddOptions(EnumType);
        
        dropdown.onValueChanged.AddListener(index =>
        {
            setValue(OnDropdownValueChanged<T>(index));
        });
    }
    
    public T OnDropdownValueChanged<T>(int index) where T : Enum
    {
        return (T)Enum.ToObject(typeof(T), index);
    }
    
    public void Testpe()
    {
        if (againItem != null)
        {
            againItem.UseItem();
            againItem = null;
        }
        
        StartCoroutine(Testpp());
    }
    public IEnumerator Testpp()
    {
        eventTriggered = false;
        isAgain = false;
        
        var slot = Gacha(test1);
        
        Debug.Log($"{slot.Col}, {slot.Row}");
        
        if (againGacha)
        {
            testEvent += OnEvent;
            
            yield return new WaitUntil(()=> eventTriggered);
            
            againGacha = false;
            testEvent -= OnEvent;
        }
        
        if (isAgain)
        {
            
            StartCoroutine(Testpp());
            yield break;
        }
        
        ResetItem();
        slot.CountUP();
        
        slot.count.text = slot.countnum.ToString();
    }
    
    void OnEvent()
    {
        eventTriggered = true;
    }
    
    public void OnTestButton(bool adf)
    {
        isAgain = adf;
        testEvent?.Invoke();
    }
    
    
    public void UseItems()
    {
        foreach(var row in SlotList)
        {
            foreach(var slot in row.Value)
            {
                if (slot.currentitem != null)
                {
                    slot.currentitem.UseItem(slot);
                }
            }
        }
    }
    
    public void ResetItem()
    {
        foreach(var row in SlotList)
        {
            foreach(var slot in row.Value)
            {
                if (slot.currentitem != null)
                {
                    slot.currentitem.ResetSlot(slot);
                }
            }
        }
    }
    
    public BingoSlot Gacha(CellRarity CellRarity)
    {
        int total = 0;
        
        UseItems();
        
        foreach (var slot in SlotGradeList[CellRarity])
        {
            int w = GetWeight(slot);
            
            if (w > 0)
            {
                total += w;
            }
        }
        
        int r = UnityEngine.Random.Range(0, total);
        float acc = 0;
                
        foreach (var slot in SlotGradeList[CellRarity])
        {
            int w = GetWeight(slot);
            
            if (w <= 0)
            {
                continue;
            }
            
            acc += w;
            
            if (r < acc)
            {
                var s = slot.pluckSlot ? slot.pluckSlot : slot;
                return s;
            }
        }
        
        foreach (var slot in SlotGradeList[CellRarity])
            {
                if (GetWeight(slot) > 0)
                {
                    var s = slot.pluckSlot ? slot.pluckSlot : slot;
                    return s;
                }
            }
            
        
        throw new InvalidOperationException("빙고 설정이 안되었습니다");
    }
    
    public int GetWeight<T>(T slot) where T : BingoSlot
    {
        return slot.isLock ? 0 : 1;
    }
    
    public bool CheckLine(SynergyDirection line, int lineIndex = -1)
    {
        switch (line)
        {
            case SynergyDirection.Column:
                {
                    foreach (var slot in SlotList[SlotColumns[lineIndex]])
                    {
                        if (!slot.isUnlock)
                        {
                            return false;
                        }
                    }
                }
                break;
                
            case SynergyDirection.Row:
                {
                    foreach (var slot in SlotList)
                    {
                        
                        if (!slot.Value[lineIndex].isUnlock)
                        {
                            return false;
                        }
                    }
                }
                break;
                
            case SynergyDirection.Diagonal:
                {
                    int index = 0;
                    foreach (var slot in SlotList)
                    {
                        if (!slot.Value[index++].isUnlock)
                        {
                            return false;
                        }
                    }
                }
                break;
        }
        
        return true;
    }
    public BingoSlot GetSlot(int col, int row)
    {
        return SlotList[SlotColumns[col]][row];
    }
    
    public float GetSynergyStat(StatType statType)
    {
        float increaseStat = 0;
        
        foreach(var synergy in Synergies)
        {
            if (!synergy.isBingo)
            {
                continue;
            }
            
            if (synergy.StatType1 == statType)
            {
                increaseStat += synergy.IncreaseStat1;
            }
            
            else if (synergy.StatType2 == statType)
            {
                increaseStat += synergy.IncreaseStat2;
            }
        }
        
        return increaseStat;
    }
    
    public void dsafsda()
    {
        Debug.Log("빙고판 눌림");
    }
    
    public void OnClick(ItemBase bingoItem)
    {
        
        if (bingoItem == null)
        {
            return;
        }
        
        againItem = againItem == bingoItem ? null : bingoItem;
    }
    
    
}
