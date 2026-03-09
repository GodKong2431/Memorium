using System.Collections.Generic;
using UnityEngine;

public static class TypeToId
{
    static Dictionary<ItemType, int> typeToId = new Dictionary<ItemType, int>();
    public static int ConvertTypeToId(ItemType type)
    {
        if (!typeToId.ContainsKey(type))
        {
            foreach (var item in DataManager.Instance.ItemInfoDict)
            {
                if (item.Value.itemType == type)
                {
                    typeToId[type] = item.Key;
                    break;
                }
            }
        }
        return typeToId[type];
    }
}
