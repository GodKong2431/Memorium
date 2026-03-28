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
    [SerializeField] private float gachaDelay;
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
    private ParticleSystem againItemBoardEnterEffect;
    private ParticleSystem gachaPreviewEffect;
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
        
        ctx.BoardTransform.onClick.AddListener(() => OnClick(bingoItemManager.itemBase as AgainItem));
        
        // slotTest.onClick.RemoveAllListeners();
        // slotTest.onClick.AddListener(()=> Testpe());
        
        SlotList.Clear();
        SlotGradeList.Clear();
        RowSynergy.Clear();
        ColSynergy.Clear();
        Synergies.Clear();
        
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
                        var synergyRowItem = Instantiate(item.Value , ctx.RowLineSynergyTransform);
                        synergyRowItem.Init(synergy.Key, item.Key);
                        synergyRowItem.SetSynergy(GetFirstSynergy(synergyRowItem));
                        RowSynergy.Add(item.Key,synergyRowItem);
                        Synergies.Add(synergyRowItem);
                        break;
                    case SynergyDirection.Column:
                        var synergyColItem = Instantiate(item.Value , ctx.ColumnSynergyTransform);
                        synergyColItem.Init(synergy.Key, item.Key);
                        synergyColItem.SetSynergy(GetFirstSynergy(synergyColItem));
                        ColSynergy.Add(item.Key,synergyColItem);
                        Synergies.Add(synergyColItem);
                        break;
                    case SynergyDirection.Diagonal:
                        DiagSynergy = Instantiate(item.Value , ctx.DiaLineSynergyTransform);
                        DiagSynergy.Init(synergy.Key, item.Key);
                        DiagSynergy.SetSynergy(GetFirstSynergy(DiagSynergy));
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
        if (bingoSynergy == null || DataManager.Instance == null || DataManager.Instance.BoardSynergyDict == null)
            return 0;

        List<BoardSynergyTable> candidates = new List<BoardSynergyTable>();
        foreach (var entry in DataManager.Instance.BoardSynergyDict)
        {
            BoardSynergyTable table = entry.Value;
            if (table == null || table.synergyDirection != bingoSynergy.bingoSynergyLine)
                continue;

            candidates.Add(table);
        }

        if (candidates.Count == 0)
            return 0;

        if (bingoSynergy.bingoSynergyLine == SynergyDirection.Diagonal)
            return candidates[0].startSynergyId;

        int zeroBasedLine = bingoSynergy.index + 1;
        int oneBasedLine = bingoSynergy.index;

        // 프로젝트마다 index 기준(0-based/1-based)이 달라질 수 있어 둘 다 허용한다.
        foreach (BoardSynergyTable table in candidates)
        {
            if (table.lineNumber == zeroBasedLine || table.lineNumber == oneBasedLine)
                return table.startSynergyId;
        }

        candidates.Sort((a, b) => a.lineNumber.CompareTo(b.lineNumber));
        int fallbackIndex = Mathf.Clamp(bingoSynergy.index, 0, candidates.Count - 1);
        if (fallbackIndex >= candidates.Count && fallbackIndex - 1 >= 0)
            fallbackIndex -= 1;

        if (fallbackIndex >= 0 && fallbackIndex < candidates.Count)
            return candidates[fallbackIndex].startSynergyId;

        return 0;
    }

    public void RefreshSynergies()
    {
        if (Synergies == null)
            return;

        for (int i = 0; i < Synergies.Count; i++)
        {
            BingoSynergy synergy = Synergies[i];
            if (synergy == null)
                continue;

            synergy.SetSynergy(GetFirstSynergy(synergy));
            if (ctx != null && ctx.SynergyViewObjects != null && i < ctx.SynergyViewObjects.Count)
                ctx.SynergyViewObjects[i].SetView(synergy);
        }
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
        ResetForBingoUiDisable();
    }

    public void ResetForBingoUiDisable()
    {
        if (bingoItemManager != null)
            bingoItemManager.ResetForBingoUiDisable();

        if (againItemBoardEnterEffect != null && BingoEffectManager.Instance != null)
            BingoEffectManager.Instance.ReturnBoardEnterEffect(againItemBoardEnterEffect);

        againItemBoardEnterEffect = null;
        ReturnGachaPreviewEffect();
        againItem = null;
        againGacha = false;
        isAgain = false;

        foreach (var row in SlotList)
        {
            if (row.Value == null)
                continue;

            foreach (var slot in row.Value)
            {
                if (slot != null && slot.Currentitem != null)
                    slot.Currentitem = null;
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
    
    public void BingoGacha(int enumIndex, int id)
    {
        RarityType cellRarity = (RarityType)enumIndex;
        
        if (RarityType.mythic == (RarityType)enumIndex)
        {
            SlotGradeList.TryGetValue(RarityType.mythic, out var bingoSlots);
            
            int unlockCount = 0;
            
            foreach(var slot in bingoSlots)
            {
                if (slot.isUnlock)
                    unlockCount++;
            }
            
            if (bingoSlots.Count <= unlockCount)
            {
                return;
            }
            
        }
        
        if (!InventoryManager.Instance.RemoveItem(id,1))
            return;
        
        if (againItem != null)
        {
            againItem.UseItem();
            
            if (againItemBoardEnterEffect != null && BingoEffectManager.Instance != null)
            {
                BingoEffectManager.Instance.ReturnBoardEnterEffect(againItemBoardEnterEffect);
                againItemBoardEnterEffect = null;
            }
            
            againItem = null;
        }
        
        StartCoroutine(GachaStart(cellRarity));
    }
    public IEnumerator GachaStart(RarityType cellRarity)
    {
        eventTriggered = false;
        isAgain = false;
        ParticleSystem selectedBingoEffect = null;
                
        var slot = Gacha(cellRarity);
        
        // 여기서 가챠 애니메이션을 넣고싶어;
        if (cellRarity != RarityType.mythic)
            yield return StartCoroutine(PlayGachaBingoSlotAnimation(cellRarity));
        
        if (againGacha)
        {
            // 여기는 선택된 이펙트 계속 유지하는거
            if (BingoEffectManager.Instance != null && slot != null)
                selectedBingoEffect = BingoEffectManager.Instance.PlayLinkRegisterEffectManual(slot.transform);

            testEvent += OnEvent;
            OpenPopUp();
            yield return new WaitUntil(()=> eventTriggered);
            ClosePopUp();
            againGacha = false;
            testEvent -= OnEvent;
        }
        
        else
        {
            //여기는 이펙트가 끝나면 자동으로 되돌아가는거
            if (BingoEffectManager.Instance != null && slot != null)
                BingoEffectManager.Instance.PlayLinkRegisterEffect(slot.transform);
        }
        
        if (isAgain)
        {
            // 선택된 빙고 애니메이션 종료 자리
            if (BingoEffectManager.Instance != null && selectedBingoEffect != null)
                BingoEffectManager.Instance.ReturnLinkRegisterEffect(selectedBingoEffect);
            StartCoroutine(GachaStart(cellRarity));
            yield break;
        }

        if (BingoEffectManager.Instance != null && selectedBingoEffect != null)
            BingoEffectManager.Instance.ReturnLinkRegisterEffect(selectedBingoEffect);
        
        ResetItem();
        slot.CountUP(1);
        
        //여기서 저장
    }
    
    void OnEvent()
    {
        eventTriggered = true;
    }

    private IEnumerator PlayGachaBingoSlotAnimation(RarityType cellRarity)
    {
        if (BingoEffectManager.Instance == null)
            yield break;

        if (!SlotGradeList.TryGetValue(cellRarity, out var raritySlots) || raritySlots == null || raritySlots.Count == 0)
            yield break;

        List<BingoSlot> candidates = new List<BingoSlot>();
        foreach (var slot in raritySlots)
        {
            if (slot == null || slot.isLock)
                continue;

            candidates.Add(slot);
        }

        if (candidates.Count == 0)
            yield break;

        float totalDuration = Mathf.Max(0f, gachaDelay);
        if (totalDuration <= 0f)
            yield break;

        float elapsed = 0f;
        const float previewTick = 0.06f;

        try
        {
            while (elapsed < totalDuration)
            {
                ReturnGachaPreviewEffect();

                BingoSlot previewSlot = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                if (previewSlot != null)
                    gachaPreviewEffect = BingoEffectManager.Instance.PlayGachaBingoSlotManual(previewSlot.transform);

                float wait = Mathf.Min(previewTick, totalDuration - elapsed);
                if (wait <= 0f)
                    break;

                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }
        }
        finally
        {
            ReturnGachaPreviewEffect();
        }
    }

    private void ReturnGachaPreviewEffect()
    {
        if (gachaPreviewEffect == null)
            return;

        if (BingoEffectManager.Instance != null)
            BingoEffectManager.Instance.ReturnGachaBingoSlot(gachaPreviewEffect);

        gachaPreviewEffect = null;
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
                if (slot.Currentitem != null)
                {
                    slot.Currentitem.UseItem(slot);
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
                if (slot.Currentitem != null)
                {
                    InventoryManager.Instance.RemoveItem(slot.Currentitem.itemInfoID, 1);
                    slot.Currentitem.ResetSlot(slot);
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

    public Transform GetBingoButtonTransformByItemId(int itemId)
    {
        if (ctx == null || ctx.BingoButtons == null)
            return null;

        int index = itemId - 3410001;
        if (index < 0 || index >= ctx.BingoButtons.Count)
            return null;

        TextMeshProUGUI buttonText = ctx.BingoButtons[index];
        if (buttonText == null)
            return null;

        Button button = buttonText.GetComponentInParent<Button>();
        return button != null ? button.transform : buttonText.transform;
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
        //여기서 aginItem 등록되면 이펙트를 호출하고 등록이 끝나거나 null이 들어오면 돌려보내(이펙트 효과는 계속 유지 되어야해)

        ItemBase nextAgainItem = bingoItem == null ? null : (againItem == bingoItem ? null : bingoItem);

        if (againItemBoardEnterEffect != null && BingoEffectManager.Instance != null)
        {
            BingoEffectManager.Instance.ReturnBoardEnterEffect(againItemBoardEnterEffect);
            againItemBoardEnterEffect = null;
        }

        againItem = nextAgainItem;

        if (againItem != null && BingoEffectManager.Instance != null)
        {
            Transform target = ctx != null && ctx.BoardTransform != null
                ? ctx.BoardTransform.transform
                : againItem.transform;

            againItemBoardEnterEffect = BingoEffectManager.Instance.PlayBoardEnterEffectManual(target);
        }
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
