using AYellowpaper.SerializedCollections;
using Mono.Cecil.Cil;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class SynergyManager : Singleton<SynergyManager>
{
    public static event Action<bool> OnSynergyGachaRunningChanged;

    [SerializeField] private float synergGachaDelay;
    public SynergyItem item;
    
    public Button button;
    
    public SerializedDictionary<int, SynergyData> synergyDatas;
    public SerializedDictionary<RarityType, DustData> synergyDustDatas;
    public SynergyDataSO synergyDataSo;
    
    public BingoSynergy currentSynergy;
    private ParticleSystem gachaSynergyPreviewEffect;
    
    event Action testEvent;
    bool eventTriggered;
    
    bool isAceppt;
        
    public RetryUI retryUI;
    public bool IsSynergyGachaRunning { get; private set; }
    private int activeSynergyGachaRoutineCount;
    
    public static event Action OnOpenPopUp;
    
    public event Action<BingoSynergy> OnChangedSynergy;
    public float GetSynergyStat(StatType statType)
    {
        return 0f;
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);

        if (synergyDatas == null)
            synergyDatas = new SerializedDictionary<int, SynergyData>();
        else
            synergyDatas.Clear();
        
        int id = 3431001;
        int plusid = 1;
        
        foreach(var data in synergyDataSo.SynergyDataDict)
        {
            foreach(var synergydata in data.Value)
            {
                synergydata.Value.LoadSynergy(id);
                synergyDatas.Add(id, synergydata.Value);
                id += 1000;
            }
            id = 3431001;
            id += plusid++;
        }
        
        BuildDustDataCache();

        if (BingoBoardManager.Instance != null)
            BingoBoardManager.Instance.RefreshSynergies();
    }

    void OnEnable()
    {
        SynergyUI.OnSynergyGachaButton += SynergyGacha;
    }

    void OnDisable()
    {
        SynergyUI.OnSynergyGachaButton -= SynergyGacha;
        activeSynergyGachaRoutineCount = 0;
        SetSynergyGachaRunning(false);
    }

    private void BuildDustDataCache()
    {
        if (synergyDustDatas == null)
            synergyDustDatas = new SerializedDictionary<RarityType, DustData>();
        else
            synergyDustDatas.Clear();

        // CSV(DustTable)에서 직접 가져와 캐시한다.
        if (DataManager.Instance != null && DataManager.Instance.DustDict != null)
        {
            foreach (var data in DataManager.Instance.DustDict)
            {
                DustTable dustTable = data.Value;
                if (dustTable == null)
                    continue;

                DustData dustData = new DustData
                {
                    dustCost = dustTable.dustCost,
                    dustProvided = dustTable.dustProvided,
                };

                if (synergyDustDatas.ContainsKey(dustTable.synergyRarity))
                    synergyDustDatas[dustTable.synergyRarity] = dustData;
                else
                    synergyDustDatas.Add(dustTable.synergyRarity, dustData);
            }
        }

        // 호환용 fallback: SO 값이 있는 프로젝트에서는 SO 값도 반영.
        if (synergyDustDatas.Count == 0 && synergyDataSo != null && synergyDataSo.SynergyDustDataDict != null)
        {
            foreach (var data in synergyDataSo.SynergyDustDataDict)
            {
                if (data.Value == null)
                    continue;

                DustData dustData = new DustData
                {
                    dustCost = data.Value.dustCost,
                    dustProvided = data.Value.dustProvided,
                };

                if (synergyDustDatas.ContainsKey(data.Key))
                    synergyDustDatas[data.Key] = dustData;
                else
                    synergyDustDatas.Add(data.Key, dustData);
            }
        }
    }

    public bool TryGetDustData(RarityType rarityType, out DustData dustData)
    {
        dustData = null;

        if (synergyDustDatas != null && synergyDustDatas.TryGetValue(rarityType, out var cachedData))
        {
            dustData = cachedData;
            return true;
        }

        if (DataManager.Instance != null && DataManager.Instance.DustDict != null)
        {
            foreach (var data in DataManager.Instance.DustDict)
            {
                DustTable table = data.Value;
                if (table == null || table.synergyRarity != rarityType)
                    continue;

                dustData = new DustData
                {
                    dustCost = table.dustCost,
                    dustProvided = table.dustProvided,
                };
                return true;
            }
        }

        return false;
    }

    public bool TryGetSynergyData(int synergyId, out SynergyData synergyData)
    {
        synergyData = null;

        if (synergyId <= 0)
            return false;

        if (synergyDatas != null &&
            synergyDatas.TryGetValue(synergyId, out var cachedData) &&
            cachedData != null)
        {
            synergyData = cachedData;
            return true;
        }

        if (TryResolveSynergyDataFromSo(synergyId, out var resolvedData))
        {
            CacheSynergyData(synergyId, resolvedData);
            synergyData = resolvedData;
            return true;
        }

        if (TryCreatePlaceholderSynergyData(synergyId, out var placeholderData))
        {
            CacheSynergyData(synergyId, placeholderData);
            synergyData = placeholderData;
            return true;
        }

        return false;
    }

    public bool TryGetAnySynergyData(out SynergyData synergyData)
    {
        synergyData = null;

        if (synergyDatas != null)
        {
            foreach (var pair in synergyDatas)
            {
                if (pair.Value == null)
                    continue;

                synergyData = pair.Value;
                return true;
            }
        }

        if (synergyDataSo == null || synergyDataSo.SynergyDataDict == null)
            return false;

        foreach (var statEntry in synergyDataSo.SynergyDataDict)
        {
            foreach (var rarityEntry in statEntry.Value)
            {
                if (rarityEntry.Value == null)
                    continue;

                synergyData = rarityEntry.Value;
                return true;
            }
        }

        if (TryCreatePlaceholderSynergyData(3431001, out var placeholderData))
        {
            CacheSynergyData(placeholderData.ID, placeholderData);
            synergyData = placeholderData;
            return true;
        }

        return false;
    }

    private bool TryResolveSynergyDataFromSo(int synergyId, out SynergyData synergyData)
    {
        synergyData = null;

        if (synergyDataSo == null || synergyDataSo.SynergyDataDict == null)
            return false;

        foreach (var statEntry in synergyDataSo.SynergyDataDict)
        {
            foreach (var rarityEntry in statEntry.Value)
            {
                SynergyData candidate = rarityEntry.Value;
                if (candidate == null)
                    continue;

                bool isDirectIdMatch = candidate.ID == synergyId;
                bool isPatternMatch = IsSynergyIdMatch(synergyId, statEntry.Key, rarityEntry.Key);
                if (!isDirectIdMatch && !isPatternMatch)
                    continue;

                // CSV가 로딩된 뒤에는 최신 수치로 다시 맞춘다.
                if (DataManager.Instance != null &&
                    DataManager.Instance.DataLoad &&
                    candidate.ID != synergyId)
                {
                    candidate.LoadSynergy(synergyId);
                }

                synergyData = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool IsSynergyIdMatch(int synergyId, SynergyStat stat, RarityType rarity)
    {
        int statId = synergyId % 1000;
        int rarityId = ((synergyId / 1000) % 10) - 1;
        return statId == (int)stat && rarityId == (int)rarity;
    }

    private void CacheSynergyData(int synergyId, SynergyData synergyData)
    {
        if (synergyData == null)
            return;

        if (synergyDatas == null)
            synergyDatas = new SerializedDictionary<int, SynergyData>();

        if (synergyDatas.ContainsKey(synergyId))
            synergyDatas[synergyId] = synergyData;
        else
            synergyDatas.Add(synergyId, synergyData);
    }

    private bool TryCreatePlaceholderSynergyData(int synergyId, out SynergyData synergyData)
    {
        synergyData = null;
        if (synergyId <= 0)
            return false;

        int statId = synergyId % 1000;
        if (!Enum.IsDefined(typeof(SynergyStat), statId) || statId == (int)SynergyStat.None)
            statId = (int)SynergyStat.ATK;

        int rarityIndex = Mathf.Clamp(((synergyId / 1000) % 10) - 1, 0, (int)RarityType.mythic);
        RarityType rarity = (RarityType)rarityIndex;

        SynergyData placeholder = new SynergyData();
        placeholder.Init((SynergyStat)statId);
        placeholder.ID = synergyId;
        placeholder.rarityType = rarity;
        placeholder.statUp1 = 0.06f + (0.01f * rarityIndex);
        placeholder.StatUp2 = placeholder.statType2 != StatType.None ? placeholder.statUp1 : 0f;

        if (DataManager.Instance != null &&
            DataManager.Instance.DataLoad &&
            DataManager.Instance.SynergyDict != null &&
            DataManager.Instance.SynergyDict.ContainsKey(synergyId))
        {
            placeholder.LoadSynergy(synergyId);
        }

        synergyData = placeholder;
        return true;
    }

    public BingoSynergy Gacha(List<BingoSynergy> synergies)
    {
        
        int total = 0;
                
        foreach (var slot in synergies)
        {
            int w = GetWeight(slot);
            
            if (w > 0)
            {
                total += w;
            }
        }
        
        int r = UnityEngine.Random.Range(0, total);
        float acc = 0;
                
        foreach (var slot in synergies)
        {
            int w = GetWeight(slot);
            
            if (w <= 0)
            {
                continue;
            }
            
            acc += w;
            
            if (r < acc)
            {
                
                return slot;
            }
        }
        
        foreach (var slot in synergies)
            {
                if (GetWeight(slot) > 0)
                {
                    return slot;
                }
            }
            
        
        throw new InvalidOperationException("빙고 설정이 안되었습니다");
    }
    
    public int GetWeight(BingoSynergy bingoSynergy)
    {
        return 1;
    }
    
    public IEnumerator Testppt()
    {
        BeginSynergyGacha();
        try
        {
            eventTriggered = false;
            isAceppt = false;
            ParticleSystem selectedSynergyEffect = null;

            BingoBoardManager boardManager = BingoBoardManager.Instance;
            if (boardManager == null || boardManager.Synergies == null || boardManager.Synergies.Count == 0)
                yield break;

            List<BingoSynergy> synergies = boardManager.Synergies;
            currentSynergy = Gacha(synergies);

            yield return StartCoroutine(PlayGachaSynergySlotAnimation(synergies));
        
            testEvent += OnEvent;
        
            // 선택된 자리 애니메이션
            if (BingoEffectManager.Instance != null && currentSynergy != null)
                selectedSynergyEffect = BingoEffectManager.Instance.PlaySynergyRegisterEffectManual(currentSynergy.transform);
        
            OpenPopup();
            
            yield return new WaitUntil(()=> eventTriggered);
            
            ClosePopup();
        
            // 선택된 자리 애니메이션 종료
            if (BingoEffectManager.Instance != null && selectedSynergyEffect != null)
                BingoEffectManager.Instance.ReturnSynergyRegisterEffect(selectedSynergyEffect);
            testEvent -= OnEvent;
        
            if (!isAceppt)
            {
                yield break;
            }
        
            currentSynergy.SynergyData = item.synergyData;
        
            OnChangedSynergy?.Invoke(currentSynergy);
            //시너지 변경 시점
        }
        finally
        {
            EndSynergyGacha();
        }
    }

    public void TestSyer()
    {
        if (item == null || item.synergyData == null)
            return;
        
        if (!InventoryManager.Instance.RemoveItem(item.synergyData.ID,1))
            return;
        
        StartCoroutine(Testppt());
    }
    
    void OnEvent()
    {
        eventTriggered = true;
    }

    private IEnumerator PlayGachaSynergySlotAnimation(List<BingoSynergy> synergies)
    {
        if (BingoEffectManager.Instance == null || synergies == null || synergies.Count == 0)
            yield break;

        List<BingoSynergy> candidates = new List<BingoSynergy>();
        foreach (var synergy in synergies)
        {
            if (synergy == null)
                continue;

            if (GetWeight(synergy) <= 0)
                continue;

            candidates.Add(synergy);
        }

        if (candidates.Count == 0)
            yield break;

        float totalDuration = Mathf.Max(0f, synergGachaDelay);
        if (totalDuration <= 0f)
            yield break;

        float elapsed = 0f;
        const float previewTick = 0.06f;

        try
        {
            while (elapsed < totalDuration)
            {
                ReturnGachaSynergyPreviewEffect();

                BingoSynergy previewSynergy = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                if (previewSynergy != null)
                    gachaSynergyPreviewEffect = BingoEffectManager.Instance.PlayGachaSynergySlotManual(previewSynergy.transform);

                float wait = Mathf.Min(previewTick, totalDuration - elapsed);
                if (wait <= 0f)
                    break;

                yield return new WaitForSeconds(wait);
                elapsed += wait;
            }
        }
        finally
        {
            ReturnGachaSynergyPreviewEffect();
        }
    }

    private void ReturnGachaSynergyPreviewEffect()
    {
        if (gachaSynergyPreviewEffect == null)
            return;

        if (BingoEffectManager.Instance != null)
            BingoEffectManager.Instance.ReturnGachaSynergySlot(gachaSynergyPreviewEffect);

        gachaSynergyPreviewEffect = null;
    }
    
    public void TestButton(bool asd)
    {
        isAceppt = asd;
        
        testEvent?.Invoke();
    }
    
    void OpenPopup()
    {
        retryUI.gameObject.SetActive(true);
        OnOpenPopUp?.Invoke();
    }
    
    void ClosePopup()
    {
        retryUI.gameObject.SetActive(false);
    }
    
    public void SynergyGacha(int enumIndex)
    {
        RarityType synergyRarity = (RarityType)enumIndex;

        List<SynergyData> candidates = new List<SynergyData>();
        foreach (var data in synergyDatas)
        {
            SynergyData synergyData = data.Value;
            if (synergyData == null)
                continue;

            if (synergyData.rarityType != synergyRarity)
                continue;

            candidates.Add(synergyData);
        }

        if (candidates.Count == 0)
            return;

        if (!TryGetDustData(synergyRarity, out var dustData))
            return;

        if (!InventoryManager.Instance.RemoveItem(3450001, dustData.dustCost))
            return;

        SynergyData selected = candidates[UnityEngine.Random.Range(0, candidates.Count)];
        InventoryManager.Instance.AddItem(selected.ID, 1);
    }

    private void BeginSynergyGacha()
    {
        activeSynergyGachaRoutineCount++;
        if (activeSynergyGachaRoutineCount == 1)
            SetSynergyGachaRunning(true);
    }

    private void EndSynergyGacha()
    {
        activeSynergyGachaRoutineCount = Mathf.Max(0, activeSynergyGachaRoutineCount - 1);
        if (activeSynergyGachaRoutineCount == 0)
            SetSynergyGachaRunning(false);
    }

    private void SetSynergyGachaRunning(bool isRunning)
    {
        if (IsSynergyGachaRunning == isRunning)
            return;

        IsSynergyGachaRunning = isRunning;
        OnSynergyGachaRunningChanged?.Invoke(isRunning);
    }
    
}
