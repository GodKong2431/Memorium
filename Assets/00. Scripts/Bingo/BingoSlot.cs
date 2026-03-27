using JetBrains.Annotations;
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
    
    [SerializeField] RarityType currentType;
    
    [SerializeField] private int maxLink = 5;
    
    [SerializeField] private int row;
    [SerializeField] private int col;
    
    [SerializeField] private int _countnum = 0;
    
    [SerializeField] private int linkItemId;
    
    public int Countnum
    {
        get {return _countnum;}
        set
        {
            _countnum = value;
            
            if(maxLink <= _countnum && currentType != RarityType.mythic)
            {
                NextLinkItemUP();
                ResetCount();
            }
        }
    }
    
    [SerializeField] public Button button;
    
    public BingoSlot pluckSlot;
    
    public int Row {get{return row;}}
    public int Col {get{return col;}}
    public event Action UpdateUnlock;
    
    [SerializeField] private bool _isUnlock;
    [SerializeField] private Image blackImage;
        
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
            if (blackImage != null)
            {
                blackImage.gameObject.SetActive(false);
            }
            
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
            
            if (itemSprite == null)
                return;
            
            if (_currentitem != null && _currentitem.itemSprite != null)
            {
                
            }
            
            itemSprite.sprite = null;
        }
    }
    
    [SerializeField] public RarityType bingoGrade;
    
    private BingoBoardManager mgr;
    
    public bool isLock;

    void Awake()
    {
        button = GetComponent<Button>();
    }

    void Start()
    {
        mgr = BingoBoardManager.Instance;
        button.onClick.AddListener(() => OnClick(mgr.bingoItemManager.itemBase));
    }
    public void Init(int col, int row)
    {
        this.row = row;
        this.col = col;
    }
    
    public void CountUP(int index)
    {
        if (!isUnlock)
        {
            isUnlock = true;
        }
        
        Countnum += index;
        
    }
    public void ReCall()
    {
        InventoryManager.Instance.AddItem(linkItemId, Countnum);
        Countnum = 0;
        //인벤토리 연동
    }
    
    public void OnClick(ItemBase bingoItem)
    {
        Debug.Log("빙고 슬롯 눌림");
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
            foreach(var item in BingoBoardManager.Instance.bingoItemManager.pluckItems)
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
        return Countnum;
    }
    
    public void ResetCount()
    {
        Countnum = 0;
    }
    
    public void NextLinkItemUP()
    {
        if (currentType == RarityType.mythic)
            return;
            
        int NextLinkItemId = linkItemId + 1;
        InventoryManager.Instance.AddItem(NextLinkItemId, 1);
    }
}
