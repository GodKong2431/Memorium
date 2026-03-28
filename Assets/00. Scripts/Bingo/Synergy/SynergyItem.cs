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
    [SerializeField] private TextMeshProUGUI dismantleCountText;
    [SerializeField] public Button itemButton;
    
    [SerializeField] private HoldAcceleratorAddon holdAcceleratorAddon;
    
    [SerializeField] public int dismantleCount = 0;
    
    
    public void LoadSynergy(RarityType rarityKey, SynergyStat SynergyKey)
    {
        SynergyManager.Instance.synergyDataSo.SynergyDataDict.TryGetValue(SynergyKey, out var SynergyStatData);
        
        SynergyStatData.TryGetValue(rarityKey, out synergyData);
        
        increaseStatText.text = $"+ {synergyData.statUp1 * 100}%";
        currentCountText.text = $"{InventoryManager.Instance.GetItemAmount(synergyData.ID).ToFloat()}";
        
        statImage.sprite = IconManager.GetSynergyIcon(SynergyKey);
        
    }
    public void SetButton(SynergyUI synergyUI, bool toggle)
    {
        itemButton.onClick.RemoveAllListeners();
        
        if (!toggle)
        {
            itemButton.onClick.AddListener(()=> synergyUI.currentItem = this);
            DismantleCountReset();
            holdAcceleratorAddon.enabled = false;
        }
        
        else
        {
            itemButton.onClick.AddListener(()=> DismantleCountUP());
            DismantleCountReset();
            holdAcceleratorAddon.enabled = true;
        }
    }
    
    void OnEnable()
    {
        InventoryManager.Instance.OnItemAmountChanged += UpdateItemCount;
        UpdateDismantleCountText();
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAmountChanged -= UpdateItemCount;
        }
        
    }

    public void UpdateItemCount(InventoryItemContext item, BigDouble amount)
    {
        if (item.ItemId != synergyData.ID)
            return;
        
        currentCountText.text = $"{InventoryManager.Instance.GetItemAmount(item.ItemId).ToFloat()}";
    }
    
    public void DismantleCountReset()
    {
        dismantleCount = 0;
        UpdateDismantleCountText();
    }
    
    public void DismantleCountUP()
    {
        if (dismantleCount >= InventoryManager.Instance.GetItemAmount(synergyData.ID).ToFloat())
            return;
        
        dismantleCount++;
        UpdateDismantleCountText();
        
    }
    
    public void UpdateDismantleCountText()
    {
        dismantleCountText.text = dismantleCount <= 0 ? "" : $"(-{dismantleCount})";
    }
    
    public void DismantleSynergy()
    {
        if (dismantleCount == 0)
            return;

        if (SynergyManager.Instance == null ||
            !SynergyManager.Instance.TryGetDustData(synergyData.rarityType, out var dustData))
            return;

        if(!InventoryManager.Instance.RemoveItem(synergyData.ID, dismantleCount))
            return;

        InventoryManager.Instance.AddItem(3450001, dustData.dustProvided * dismantleCount);
        
        DismantleCountReset();
    }
}
