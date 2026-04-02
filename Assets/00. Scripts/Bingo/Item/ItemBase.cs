using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class ItemBase : MonoBehaviour
{
    [SerializeField] public BingoItemType itemType;
    
    [SerializeField] public Image itemSprite;

    [SerializeField] public int itemCount;
    
    [SerializeField] public TextMeshProUGUI itemCountText;
    
    [SerializeField] public int itemInfoID;
    
    [SerializeField] public Toggle Itemtoggle;
    [SerializeField] public BingoBoardManager mgr ;
    
    [SerializeField] public BingoItemManager itemMgr;
    
    [SerializeField] private BingoSlot currentSlot;
    
    public BingoSlot CurrentSlot
    {
        get {return currentSlot;}
        set
        {
            currentSlot = value;
        }
    }

    void Awake()
    {
        Itemtoggle = GetComponent<Toggle>();
    }

    public virtual void Start()
    {
        Init();
        itemCountText.text = $"{InventoryManager.Instance.GetItemAmount(itemInfoID)}";
    }

    void OnEnable()
    {
        InventoryManager.Instance.OnItemAmountChanged += UpdateUI;
    }

    void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAmountChanged -= UpdateUI;
            Reset();
        }
        
    }
    
    public void UpdateUI(InventoryItemContext item, BigDouble amount)
    {
            
        if (item.ItemId != itemInfoID)
        
            return;
        
        itemCountText.text = $"{InventoryManager.Instance.GetItemAmount(item.ItemId)}";
    }
    
    public abstract void UseItem(BingoSlot bingoSlot = null);
    
    public virtual void ResetSlot(BingoSlot bingoSlot)
    {
        bingoSlot.Currentitem = null;
    }
    
    public virtual void Init()
    {
        mgr = BingoBoardManager.Instance;
        Itemtoggle.onValueChanged.AddListener(_ => itemMgr.itemBase = _? this : null);
        
        DataManager.Instance.ItemInfoDict.TryGetValue(itemInfoID, out var table);
        
        string icon = table.itemIcon.Replace("Assets/Resources/", "").Replace(".png", "");
                
        itemSprite.sprite = Resources.Load<Sprite>(icon);
    }
    
    public void Reset()
    {
        if (Itemtoggle == null)
            return;

        Itemtoggle.SetIsOnWithoutNotify(false);

        if (itemMgr != null && itemMgr.itemBase == this)
            itemMgr.itemBase = null;
    }
}
