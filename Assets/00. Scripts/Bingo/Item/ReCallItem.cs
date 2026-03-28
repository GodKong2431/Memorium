using UnityEngine;

public class ReCallItem : ItemBase
{
    public override void UseItem(BingoSlot bingoSlot)
    {
        InventoryManager.Instance.RemoveItem(itemInfoID, 1);

        //여기 첫번째 이펙트(효과 끝나면 사라지는)
        if (BingoEffectManager.Instance != null && bingoSlot != null)
            BingoEffectManager.Instance.PlayRecallItemPrimaryEffect(bingoSlot.transform);

        bingoSlot.ReCall();
    }
}
