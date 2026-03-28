using UnityEngine;

public class LockItem : ItemBase
{
    public Sprite spriteImage;
    
    public override void UseItem(BingoSlot bingoSlot)
    {
        bingoSlot.isLock = true;
    }

    public override void ResetSlot(BingoSlot bingoSlot)
    {
        bingoSlot.isLock = false;
        base.ResetSlot(bingoSlot);
    }
}
