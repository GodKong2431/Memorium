using System;
using System.Collections.Generic;

[System.Serializable]
public class ItemsTable : TableBase
{
    public ItemType itemType;
    public string itemName;
    public bool isStack;
    public string desc;
}
