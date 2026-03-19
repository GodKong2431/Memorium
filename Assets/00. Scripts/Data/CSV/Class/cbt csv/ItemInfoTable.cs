using System;
using System.Collections.Generic;

[System.Serializable]
public class ItemInfoTable : TableBase
{
    public string itemTypedesc;
    public ItemType itemType;
    public string itemName;
    public string itemDesc;
    public bool isStack;
    public string desc;
    public string itemIcon;
}
