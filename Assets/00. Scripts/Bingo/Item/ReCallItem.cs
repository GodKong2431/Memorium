using UnityEngine;

public class ReCallItem : ItemBase
{
    public override void UseItem(BingoSlot bingoSlot)
    {
        //인벤토리 연동
        bingoSlot.ReCall();
    }
}
