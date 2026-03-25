using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BingoSlot : MonoBehaviour
{
    [SerializeField] private ItemBase _currentitem;
    
    [SerializeField] private Image itemSprite;
    
    [SerializeField] List<BingoLink> currentLink = new List<BingoLink>();
    
    [SerializeField] private int maxLink = 5;
    
    [SerializeField] private int row;
    [SerializeField] private int col;
    
    [SerializeField] public int countnum = 0;
    
    [SerializeField] public Button button;
    
    public BingoSlot pluckSlot;
    
    public int Row {get{return row;}}
    public int Col {get{return col;}}
    public event Action UpdateUnlock;
    
    [SerializeField] private bool _isUnlock;
        
    public bool isUnlock
    {
        get
        {
            return _isUnlock;
        }
        
        set
        {
            if (_isUnlock == value) return;
            
            _isUnlock = value;
            UpdateUnlock?.Invoke();
        }
    }
    
    public ItemBase currentitem
    {
        get {return _currentitem;}
        set
        {
            if (value == null)
            {
                _currentitem.CurrentSlot = null;
            }
            
            if(value != null)
            {
                if (_currentitem != null)
                {
                    _currentitem.CurrentSlot = null;
                }
                
                if(value.CurrentSlot != null)
                {
                    value.CurrentSlot.currentitem = null;
                    value.CurrentSlot = null;
                }
                value.CurrentSlot = this;
            }
            
            _currentitem = value;
            
            if (_currentitem != null)
            {
                itemSprite.sprite = _currentitem.itemSprite;
                return;
            }
            
            itemSprite.sprite = null;
        }
    }
    
    [SerializeField] public RarityType bingoGrade;
    
    private BingoBoard mgr;
    
    public bool isLock;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        mgr = BingoBoard.Instance;
        button.onClick.AddListener(() => OnClick(mgr.bingoItemManager.itemBase));
    }
    public void Init(int col, int row)
    {
        this.row = row;
        this.col = col;
    }
    
    public void CountUP()
    {
        if (!isUnlock)
        {
            isUnlock = true;
        }
        
        countnum++;
        
    }
    public void ReCall()
    {
        countnum = 0;
        //인벤토리 연동
    }
    
    public void OnClick(ItemBase bingoItem)
    {
        
        if (bingoItem == null)
        {
            return;
        }
        
        if(bingoItem as ReCallItem)
        {
            bingoItem.UseItem(this);
            return;
        }
        
        if(bingoItem is PluckItem pluckItem)
        {
            if(!pluckItem.IsWithinBounds(row, col))
                return;
            foreach(var item in BingoBoard.Instance.bingoItemManager.pluckItems)
            {
                if(pluckItem != item)
                {
                    item.ResetSlot();
                }
            }
        }
        
        currentitem = currentitem == bingoItem ? null : bingoItem;
    }
    
    public int ReturnCount()
    {
        return countnum;
    }
    
    public void ResetCount()
    {
        countnum = 0;
    }
}
