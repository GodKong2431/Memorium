using System;
using System.Collections.Generic;

[System.Serializable]
public class BoardCellTable : TableBase
{
    public string cellRarityDesc;
    public int cellLocationX;
    public int cellLocationY;
    public RarityType cellRarity;
    public int maxStack;
}
