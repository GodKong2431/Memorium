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
    private ParticleSystem currentPluckEffect;
    
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
    
    public ItemBase Currentitem
    {
        get {return _currentitem;}
        set
        {
            if (_currentitem is PluckItem && _currentitem != value)
            {
                ReturnPluckEffect();
            }

            if (value == null && _currentitem != null)
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
                    value.CurrentSlot.Currentitem = null;
                    value.CurrentSlot = null;
                }
                value.CurrentSlot = this;
            }
            
            _currentitem = value;

            if (_currentitem is PluckItem pluckItem && BingoEffectManager.Instance != null)
            {
                if (currentPluckEffect != null)
                {
                    BingoEffectManager.Instance.ReturnPluckItemEffect(currentPluckEffect);
                    currentPluckEffect = null;
                }

                currentPluckEffect = BingoEffectManager.Instance.PlayPluckItemEffectManual(transform, pluckItem.dir);
            }
            else
            {
                ReturnPluckEffect();
            }
            
            if (_currentitem != null)
            {
                SoundManager.Instance.PlayUiSfx(9100069);
            }
            else
            {
                if (SoundManager.Instance != null)
                    SoundManager.Instance.PlayUiSfx(9100070);
            }
            
            if (itemSprite == null)
                return;

            Sprite slotSprite = null;
            if (_currentitem is LockItem lockItem)
            {
                if (lockItem.spriteImage != null)
                    slotSprite = lockItem.spriteImage;
                else if (_currentitem.itemSprite != null)
                    slotSprite = _currentitem.itemSprite.sprite;
            }

            itemSprite.sprite = slotSprite;
            itemSprite.enabled = slotSprite != null;

            Color color = itemSprite.color;
            color.a = slotSprite != null ? 1f : 0f;
            itemSprite.color = color;
        }
    }

    private void ReturnPluckEffect()
    {
        if (currentPluckEffect == null)
            return;

        if (BingoEffectManager.Instance != null)
            BingoEffectManager.Instance.ReturnPluckItemEffect(currentPluckEffect);

        currentPluckEffect = null;
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
        if (button != null)
            button.onClick.AddListener(HandleSlotClick);
    }

    private void HandleSlotClick()
    {
        if (mgr == null)
            mgr = BingoBoardManager.Instance;

        ItemBase selectedItem = null;
        if (mgr != null && mgr.bingoItemManager != null)
            selectedItem = mgr.bingoItemManager.itemBase;

        OnClick(selectedItem);
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

        if (BingoEffectManager.Instance != null && BingoBoardManager.Instance != null)
        {
            Transform target = BingoBoardManager.Instance.GetBingoButtonTransformByItemId(linkItemId);
            if (target != null)
                BingoEffectManager.Instance.PlayRecallItemSecondaryEffect(target);
        }

        InventoryManager.Instance.AddItem(linkItemId, Countnum);
        Countnum = 0;
        //인벤토리 연동
    }
    
    public void OnClick(ItemBase bingoItem)
    {
        InventoryManager inventoryManager = InventoryManager.Instance;
        if (inventoryManager == null)
            return;

        if (bingoItem == null)
            return;
        
        if (inventoryManager.GetItemAmount(bingoItem.itemInfoID) <= 0)
        {
            return;
        }
        
        if (currentType == RarityType.mythic && bingoItem is LockItem)
            return;
        
        if (bingoItem is ReCallItem)
        {
            if (currentType == RarityType.mythic)
                return;
            
            if (Countnum <= 0)
                return;
            
            bingoItem.UseItem(this);
            
            return;
        }
        
        if (bingoItem is PluckItem pluckItem)
        {
            if (!pluckItem.IsWithinBounds(row, col))
                return;

            BingoBoardManager boardManager = BingoBoardManager.Instance;
            if (boardManager == null || boardManager.bingoItemManager == null || boardManager.bingoItemManager.pluckItems == null)
                return;

            foreach (var item in boardManager.bingoItemManager.pluckItems)
            {
                if (pluckItem != item)
                {
                    item.ResetSlot();
                }
            }
        }
        
        Currentitem = Currentitem == bingoItem ? null : bingoItem;
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

        if (BingoEffectManager.Instance != null && BingoBoardManager.Instance != null)
        {
            Transform target = BingoBoardManager.Instance.GetBingoButtonTransformByItemId(NextLinkItemId);
            if (target != null)
                BingoEffectManager.Instance.PlayRecallItemSecondaryEffect(target);
        }
    }
}
