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
    }

    public abstract void UseItem(BingoSlot bingoSlot = null);
    
    public virtual void ResetSlot(BingoSlot bingoSlot)
    {
        bingoSlot.currentitem = null;
    }
    
    public virtual void Init()
    {
        mgr = BingoBoardManager.Instance;
        Itemtoggle.onValueChanged.AddListener(_ => itemMgr.itemBase = _? this : null);
    }
    
    public void Reset()
    {
        Itemtoggle.isOn = false;
    }
}
