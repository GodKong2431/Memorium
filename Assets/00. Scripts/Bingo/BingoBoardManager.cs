using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;   

[Serializable]
public struct BingoSlotSaveData
{
    public int row; // 몇번재 줄
    public int col; // 몇번째 라인
    public int count; // 몇개 가지고 있는지
    public int rarityEnum; // 슬롯 등급이 뭔지
    public bool unlockStates; // 언락 되어 있는지
}

public struct SynergySlotSaveData
{
    public int index; // 몇번째 줄인지 
    public int enumCount; // 가로 세로 대각선인지
    public int synergyID; // 들어가있는 시너지의 ID
}

public struct BingoBoardSaveData
{
    List<BingoSlotSaveData> bingoSlotSaveDatas;
    
    List<SynergySlotSaveData> SynergySlotSaveDatas;
}

public class BingoBoardManager : Singleton<BingoBoardManager>
{
    [SerializeField] public SerializedDictionary<BingoColumnSlot, List<BingoSlot>> SlotList = new SerializedDictionary<BingoColumnSlot, List<BingoSlot>>();
    [SerializeField] private BingoContext ctx;
    [SerializeField] BingoBoardSO bingoBoardSO;
            
    [SerializeField] SerializedDictionary<RarityType, List<BingoSlot>> SlotGradeList = new SerializedDictionary<RarityType, List<BingoSlot>>();
    
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
        bingoItemManager = FindAnyObjectByType<BingoItemManager>();
    }

    public void Init(BingoContext _ctx)
    {
        ctx = _ctx;

        if (ctx == null)
        {
            return;
        }

        if (ctx.Columns == null || ctx.Columns.Count == 0)
        {
            return;
        }
        
        ctx.BoardTransform.onClick.AddListener(() => OnClick(bingoItemManager.itemBase is AgainItem item ? item : null));
        
        // slotTest.onClick.RemoveAllListeners();
        // slotTest.onClick.AddListener(()=> Testpe());
        
        SlotList.Clear();
        SlotGradeList.Clear();
        
        foreach (var slotColumn in ctx.Columns)
        {
            if (slotColumn == null)
            {
                continue;
            }

            SlotList.Add(slotColumn, new List<BingoSlot>());
        }
        
        foreach(RarityType grade in Enum.GetValues(typeof(RarityType)))
        {
            SlotGradeList.Add(grade, new List<BingoSlot>());
        }

        if (bingoBoardSO == null)
        {
            return;
        }

        var slotColumns = ctx.Columns;

        
        foreach (var synergy in bingoBoardSO.bingoSynergys)
        {
            foreach (var item in synergy.Value)
            {
                switch (synergy.Key)
                {
                    case SynergyDirection.Row:
                        item.Value.Init(synergy.Key,item.Key);
                        item.Value.SetSynergy(GetFirstSynergy(item.Value));
                        var synergyRowItem = Instantiate(item.Value , ctx.RowLineSynergyTransform);
                        RowSynergy.Add(item.Key,synergyRowItem);
                        Synergies.Add(synergyRowItem);
                        break;
                    case SynergyDirection.Column:
                        item.Value.Init(synergy.Key,item.Key);
                        item.Value.SetSynergy(GetFirstSynergy(item.Value));
                        var synergyColItem = Instantiate(item.Value , ctx.ColumnSynergyTransform);
                        ColSynergy.Add(item.Key,synergyColItem);
                        Synergies.Add(synergyColItem);
                        break;
                    case SynergyDirection.Diagonal:
                        item.Value.Init(synergy.Key,item.Key);
                        item.Value.SetSynergy(GetFirstSynergy(item.Value));
                        DiagSynergy = Instantiate(item.Value , ctx.DiaLineSynergyTransform);
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
                bingoBoardSO.RaritySolts.TryGetValue(slotColumn[row], out var bingoSlot);
                BingoSlot slotItem = Instantiate(bingoSlot , slotColumns[col].transform);
                slotColumns[col].bingoSlotDatas.Add(slotItem);
                
                slotItem.Init(col,row);
                slotItem.UpdateUnlock += RowSynergy[row].Check;
                slotItem.UpdateUnlock += ColSynergy[col].Check;
                
                if (col == row)
                {
                    slotItem.UpdateUnlock += DiagSynergy.Check;
                }
                
                SlotList[slotColumns[col]].Add(slotItem);
                SlotGradeList[slotColumn[row]].Add(slotItem);
            }
        }
        
        // 데이터 로딩
        
        for (int i = 0; i < Synergies.Count; i++)
        {
            ctx.SynergyViewObjects[i].SetView(Synergies[i]);
        }
    }

    public int GetFirstSynergy(BingoSynergy bingoSynergy)
    {
        foreach(var key in DataManager.Instance.BoardSynergyDict)
        {
            if (key.Value.synergyDirection != SynergyDirection.Diagonal)
            {
                key.Value.lineNumber -= 1;
            }
            
            if (bingoSynergy.bingoSynergyLine == key.Value.synergyDirection && bingoSynergy.index == key.Value.lineNumber)
            {
                return key.Value.startSynergyId;
            }
        }
        
        return 0;
    }
    
    void OnEnable()
    {
        synergyMgr.OnChangedSynergy += UpdateSynergyView;
        BingoUI.OnClickBingoGachaButton += BingoGacha;
    }
    
    void OnDisable()
    {
        synergyMgr.OnChangedSynergy -= UpdateSynergyView;
        BingoUI.OnClickBingoGachaButton -= BingoGacha;
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
    
    public void BingoGacha(int enumIndex, int id)
    {
        RarityType cellRarity = (RarityType)enumIndex;
        
        if (againItem != null)
        {
            againItem.UseItem();
            againItem = null;
        }
            
        InventoryManager.Instance.RemoveItem(id,1);
        
        StartCoroutine(GachaStart(cellRarity));
    }
    public IEnumerator GachaStart(RarityType cellRarity)
    {
        eventTriggered = false;
        isAgain = false;
        
        // 빙고 애니메이션 넣을 자리
        
        var slot = Gacha(cellRarity);
        
        // 선택된 빙고 애니메이션 넣을 자리
        
        Debug.Log($"{slot.Col}, {slot.Row}");
        
        if (againGacha)
        {
            testEvent += OnEvent;
            OpenPopUp();
            yield return new WaitUntil(()=> eventTriggered);
            ClosePopUp();
            againGacha = false;
            testEvent -= OnEvent;
        }
        
        // 선택된 빙고 애니메이션 종료 자리
        
        if (isAgain)
        {
            
            StartCoroutine(GachaStart(cellRarity));
            yield break;
        }
        
        ResetItem();
        slot.CountUP();
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
    
    public BingoSlot Gacha(RarityType CellRarity)
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
                    if (ctx == null || ctx.Columns == null || lineIndex < 0 || lineIndex >= ctx.Columns.Count)
                    {
                        return false;
                    }

                    foreach (var slot in SlotList[ctx.Columns[lineIndex]])
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
        if (ctx == null || ctx.Columns == null)
        {
            return null;
        }

        return SlotList[ctx.Columns[col]][row];
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
    
    public void OpenPopUp()
    {
        ctx.retry.gameObject.SetActive(true);
        ctx.retry.GetComponent<RetryUI>().SetBingoButton();
    }
    
    public void ClosePopUp()
    {
        ctx.retry.gameObject.SetActive(false);
    }
    
    public void UpdateSynergyView(BingoSynergy bingoSynergy)
    {
        int index = Synergies.IndexOf(bingoSynergy);
        ctx.SynergyViewObjects[index].SetView(bingoSynergy);
    }
}
