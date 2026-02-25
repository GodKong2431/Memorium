using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EquipmentManager :Singleton<EquipmentManager>
{
    public List<int> allEquipmentListKeys;
    public Dictionary<int, List<int>> equipmentByTierDict;
    public bool setItemDict = false;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        Init();
    }
    public void Init()
    {
        allEquipmentListKeys = new List<int>();
        allEquipmentListKeys = DataManager.Instance.EquipListDict.Keys.ToList();
        allEquipmentListKeys.Sort();

        List<int> equipmentList = new List<int>();
        equipmentByTierDict= new Dictionary<int, List<int>>();
        foreach (var key in allEquipmentListKeys)
        {
            int tier = DataManager.Instance.EquipListDict[key].equipmentTier;
            if (!equipmentByTierDict.ContainsKey(tier))
                equipmentByTierDict[tier] = new List<int>();
            //티어 단위로 아이템 설정
            equipmentByTierDict[tier].Add(key);
        }
        setItemDict = true;
    }
}
