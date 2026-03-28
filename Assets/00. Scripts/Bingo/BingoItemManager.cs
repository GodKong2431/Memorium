using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BingoItemManager : MonoBehaviour
{
    [SerializeField] private ItemBase _currentItem;
    private ParticleSystem currentItemEffectInstance;

    public ItemBase itemBase
    {
        get {return _currentItem;}
        set
        {
            if (_currentItem == value)
                return;

            if (currentItemEffectInstance != null && BingoEffectManager.Instance != null)
            {
                BingoEffectManager.Instance.ReturnItemEquipEffect(currentItemEffectInstance);
                currentItemEffectInstance = null;
            }

            _currentItem = value;

            if (_currentItem != null && BingoEffectManager.Instance != null)
            {
                Transform target = _currentItem.Itemtoggle != null
                    ? _currentItem.Itemtoggle.transform
                    : (_currentItem.itemSprite != null ? _currentItem.itemSprite.transform : _currentItem.transform);

                currentItemEffectInstance = BingoEffectManager.Instance.PlayItemEquipEffectManual(target);
            }

            OnChangedItem?.Invoke(value);
        }
    }
    public static event Action<ItemBase> OnChangedItem;
    
    public List<PluckItem> pluckItems = new List<PluckItem>();
    
    [SerializeField] private List<CanvasGroup> bingobuttons;

    void Start()
    {
        BingoBoardManager.Instance.bingoItemManager = this;
    }

    void OnDisable()
    {
        ResetForBingoUiDisable();
    }

    public void ResetForBingoUiDisable()
    {
        _currentItem = null;

        if (currentItemEffectInstance != null && BingoEffectManager.Instance != null)
            BingoEffectManager.Instance.ReturnItemEquipEffect(currentItemEffectInstance);

        currentItemEffectInstance = null;
        OnChangedItem?.Invoke(null);
    }

    public void BingoBoardClick(bool use)
    {
        foreach (var bingobutton in bingobuttons)
        {
            bingobutton.blocksRaycasts = !use;
        }
    }   
    
}
