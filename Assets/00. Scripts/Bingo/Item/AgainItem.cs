using UnityEngine;

public class AgainItem : ItemBase
{
    public override void Init()
    {
        base.Init();
        Itemtoggle.onValueChanged.AddListener(_ => itemMgr.BingoBoardClick(_));
    }

    public override void UseItem(BingoSlot bingoSlot = null)
    {
        InventoryManager.Instance.RemoveItem(itemInfoID, 1);
        mgr.againGacha = true;
    }
}
