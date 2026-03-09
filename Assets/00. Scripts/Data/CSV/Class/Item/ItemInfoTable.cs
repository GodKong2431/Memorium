using System;
using System.Collections.Generic;

[System.Serializable]
public class ItemInfoTable : TableBase
{
    public ItemType itemType;
    public string itemName;
    public bool isStack;
    public string desc;
    public string itemIcon;
}
