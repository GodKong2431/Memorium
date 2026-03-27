using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SynergyItem : MonoBehaviour
{
    public SynergyData synergyData;
    
    [SerializeField] private Image statImage;
    [SerializeField] private TextMeshProUGUI increaseStatText;
    [SerializeField] private TextMeshProUGUI currentCountText;
    [SerializeField] public Button itemButton;
    
    public void LoadSynergy(RarityType rarityKey, SynergyStat SynergyKey)
    {
        SynergyManager.Instance.synergyDataSo.SynergyDataDict.TryGetValue(SynergyKey, out var SynergyStatData);
        
        SynergyStatData.TryGetValue(rarityKey, out synergyData);
        
        increaseStatText.text = $"+ {synergyData.statUp1 * 100}%";
        currentCountText.text = $"{synergyData.count}";
        
        statImage.sprite = IconManager.GetSynergyIcon(SynergyKey);
        
        
    }
    public void SetButton(SynergyUI synergyUI)
    {
        itemButton.onClick.AddListener(()=> synergyUI.currentItem = this);
    }
    
    
    public void DismantleModeSet()
    {
    }
}
