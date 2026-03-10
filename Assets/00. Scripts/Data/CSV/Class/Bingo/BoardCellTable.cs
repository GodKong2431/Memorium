using System;
using System.Collections.Generic;

[System.Serializable]
public class BoardCellTable : TableBase
{
    public int cellLocationX;
    public int cellLocationY;
    public string cellRarity;
    public int maxStack;
}
