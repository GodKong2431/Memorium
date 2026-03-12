using Unity.VisualScripting;
using UnityEngine;

public class PluckItem : ItemBase
{
    [SerializeField] public Direction dir;


    public override void Start()
    {
        base.Start();
        mgr.bingoItemManager.pluckItems.Add(this);
    }

    public bool IsWithinBounds(int row, int col)
    {
        int boradRange = BingoBoard.Instance.BingoRange;
        
        switch (dir)
        {
            case Direction.Left:
                row--;
                return row < 0 ? false : true;
            case Direction.Right:
                row++;
                return row >= boradRange ? false : true;
            case Direction.Up:
                col++;
                return col >= boradRange ? false : true;
            case Direction.Down:
                col--;
                return col < 0 ? false : true;
            default:
                return false;
        }
    }
    
    public void ResetSlot()
    {
        if (CurrentSlot != null)
        {
            CurrentSlot.currentitem = null;
            CurrentSlot = null;
        }
    }

    public override void UseItem(BingoSlot bingoSlot)
    {
        SetSlot(bingoSlot).pluckSlot = bingoSlot;
    }

    public override void ResetSlot(BingoSlot bingoSlot)
    {
        SetSlot(bingoSlot).pluckSlot = null;
        base.ResetSlot(bingoSlot);
    }
    
    public BingoSlot SetSlot(BingoSlot bingoSlot)
    {
        int row = bingoSlot.Row;
        int col = bingoSlot.Col;
        switch (dir)
        {
            case Direction.Left:
                row--;
                break;
            case Direction.Right:
                row++;
                break;
            case Direction.Up:
                col++;
                break;
            case Direction.Down:
                col--;
                break;
        }
        
        return BingoBoard.Instance.GetSlot(col, row);
    }
}
