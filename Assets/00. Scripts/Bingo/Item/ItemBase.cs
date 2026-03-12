using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class ItemBase : MonoBehaviour
{
    [SerializeField] public BingoItemType itemType;
    
    [SerializeField] public Sprite itemSprite;

    [SerializeField] public Toggle Itemtoggle;
    [SerializeField] public BingoBoard mgr ;
    
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
        mgr = BingoBoard.Instance;
        var itemMgr = mgr.bingoItemManager;
        Itemtoggle.onValueChanged.AddListener(_ => itemMgr.itemBase = _? this : null);
    }
    
    public void Reset()
    {
        Itemtoggle.isOn = false;
    }
}
