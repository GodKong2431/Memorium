using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BingoItemManager : MonoBehaviour
{
    [SerializeField] private ItemBase _currentItem;
    public ItemBase itemBase
    {
        get {return _currentItem;}
        set
        {
            _currentItem = value;
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

    public void BingoBoardClick(bool use)
    {
        foreach (var bingobutton in bingobuttons)
        {
            bingobutton.blocksRaycasts = !use;
        }
    }   
    
}
