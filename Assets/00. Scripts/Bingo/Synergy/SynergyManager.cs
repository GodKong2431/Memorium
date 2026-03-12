using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SynergyManager : Singleton<SynergyManager>
{
    public RarityType rarity;
    public SynergyStat stat;
    
    public Button button;
    
    public SynergyData synergyData;
    public SynergyDataSO synergyDataSo;
    
    event Action testEvent;
    bool eventTriggered;
    
    bool isAceppt;
        
    public float GetSynergyStat(StatType statType)
    {
        return 0f;
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
        
        var synergy = Gacha(BingoBoard.Instance.Synergies);
        
        Debug.Log($"{synergy.bingoSynergyLine.ToString()}, {synergy.index}");
        
        testEvent += OnEvent;
            
        yield return new WaitUntil(()=> eventTriggered);
            
        testEvent -= OnEvent;
        
        if (isAceppt)
        {
            yield break;
        }
        
        synergy.SynergyData = synergyDataSo.SynergyDataDict[stat][rarity];
    }

    public void TestSyer()
    {
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
    
}
