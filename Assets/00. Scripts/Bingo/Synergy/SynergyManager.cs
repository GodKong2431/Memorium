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
    public SynergyItem item;
    
    public Button button;
    
    public SerializedDictionary<int, SynergyData> synergyDatas;
    public SerializedDictionary<RarityType, DustData> synergyDustDatas;
    public SynergyDataSO synergyDataSo;
    
    public BingoSynergy currentSynergy;
    
    event Action testEvent;
    bool eventTriggered;
    
    bool isAceppt;
        
    public RetryUI retryUI;
    
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
        
        id = 3510001;
        
        foreach(var data in synergyDataSo.SynergyDustDataDict)
        {
            
            DataManager.Instance.DustDict.TryGetValue(id, out var dustData);
            data.Value.dustCost = dustData.dustCost;
            data.Value.dustProvided = dustData.dustProvided;
            synergyDustDatas.Add(data.Key, data.Value);
            id++;
        }
        
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
        eventTriggered = false;
        isAceppt = false;
        
        currentSynergy = Gacha(BingoBoardManager.Instance.Synergies);
        
        testEvent += OnEvent;
        
        // 선택된 자리 애니메이션
        
        OpenPopup();
            
        yield return new WaitUntil(()=> eventTriggered);
            
        ClosePopup();
        
        // 선택된 자리 애니메이션 종료
        testEvent -= OnEvent;
        
        if (!isAceppt)
        {
            yield break;
        }
        
        currentSynergy.SynergyData = item.synergyData;
        
        OnChangedSynergy?.Invoke(currentSynergy);
    }

    public void TestSyer()
    {
        if (item == null)
            return;
        
        if (!InventoryManager.Instance.RemoveItem(item.synergyData.ID,1))
            return;
        
        StartCoroutine(Testppt());
    }
    
    void OnEvent()
    {
        eventTriggered = true;
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
    
}
