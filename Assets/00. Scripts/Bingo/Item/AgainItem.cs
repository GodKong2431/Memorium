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
        mgr.againGacha = true;
    }
}
