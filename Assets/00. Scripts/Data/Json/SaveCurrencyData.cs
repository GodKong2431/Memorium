using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveCurrencyData
{
    [Header("저장할 재화")]
    //현재 json 상에 딕셔너리가 저장이 안되어 사싱상 currencyTypes 의 인덱스를 키 값으로 currencyValue를 저장
    //enum값의 정수 값
    //currencyType으로 저장할 것들 ex)exp, trait
    public List<int> currencyTypes;
    //각 타입별 갯수
    public List<BigDouble> currencyValues;

    //아이디로 저장할 것들 ex) 스크롤, 픽시, 골드 등
    public List<int> itemId;
    public List<BigDouble> itemValue;

    public SerializedDictionary<CurrencyType, int> currencyTypeToKey = new SerializedDictionary<CurrencyType, int>();

    public SaveCurrencyData()
    { }


    public void InitCurrencyData()
    {
        if (currencyTypes == null)
        {
            //값 초기화
            currencyTypes = new List<int>();
            currencyValues = new List<BigDouble>();

            itemId = new List<int>();
            itemValue = new List<BigDouble>();

            //타입과 값을 같은 인덱스에 배치
            foreach (CurrencyType currencyType in Enum.GetValues(typeof(CurrencyType)))
            {
                //아이템 타입이 존재하면 따로 id가 존재하는 것이므로 넘어간다
                if (Enum.IsDefined(typeof(ItemType), (int)currencyType))
                    continue;

                currencyTypes.Add((int)currencyType);
                currencyValues.Add(BigDouble.Zero);
            }
        }


        //이후에 enum값이 추가될 가능성 대비
        if (currencyTypes.Count < Enum.GetValues(typeof(CurrencyType)).Length)
        {
            foreach (CurrencyType currencyType in Enum.GetValues(typeof(CurrencyType)))
            {
                //아이템 타입이 존재하면 따로 id가 존재하는 것이므로 넘어간다
                if (Enum.IsDefined(typeof(ItemType), (int)currencyType))
                    continue;

                //만약 해당 enum 값이 없을 경우
                if (!currencyTypes.Contains((int)currencyType))
                {
                    currencyTypes.Add((int)currencyType);
                    currencyValues.Add(BigDouble.Zero);
                }
            }
        }

        SetIndex();
    }

    public void SetIndex()
    {
        foreach (var currencyType in currencyTypes)
        {
            foreach (var item in DataManager.Instance.ItemInfoDict)
            {
                if ((int)item.Value.itemType == currencyType)
                {
                    //키 == CurrencyType 값 == 해당 타입의 아이디
                    currencyTypeToKey[(CurrencyType)currencyType] = item.Key;
                }
            }
        }
    }


    public void Save(InventoryItemContext context, BigDouble amount)
    {
        //if (!currencyTypes.Contains((int)context.ItemType))
        //    return;

        int type = (int)DataManager.Instance.ItemInfoDict[context.ItemId].itemType;
        
        //재화가 아닌 장비같은거 들어오면 반환시킨다
        if (!Enum.IsDefined(typeof(CurrencyType), (int)type))
            return;

        Debug.Log($"[SaveCurrencyData] 타입 : {context.ItemType} 이름 : {context.ItemId} 증가량 {amount}");

        int contextItemId = context.ItemId;
        if (!itemId.Contains(contextItemId))
        {
            itemId.Add(contextItemId);
            itemValue.Add(BigDouble.Zero);
        }
        int index = itemId.IndexOf(contextItemId);
        itemValue[index] = amount;


        ////값을 찾을 인덱스
        //int index = currencyTypes.IndexOf((int)context.ItemType);
        //Debug.Log($"[InventoryManager] {context.ItemType}의 인덱스 : {index} 증가량 {amount}");
        //currencyValues[index] = amount;
    }
    public void Save(CurrencyType type, BigDouble amount)
    {
        if (!currencyTypes.Contains((int)type))
            return;
        //값을 찾을 인덱스
        int index = currencyTypes.IndexOf((int)type);
        Debug.Log($"[InventoryManager] {type}의 인덱스 : {index} 증가량 {amount}");
        currencyValues[index] = amount;
    }

    ////이걸 OnApplicationQuit에서 사용할 거임 <- 어차피 데이터 값 변환시마다 변동되어 마지막에 저장 필요 x
    //public void SaveBeforeQuit(CurrencyType currencyType, BigDouble amount)
    //{
    //    //값을 찾을 인덱스
    //    int index = currencyTypes.IndexOf((int)currencyType);
    //    Debug.Log($"[InventoryManager] {currencyType}의 인덱스 : {index} 증가량 {amount}");
    //    currencyValues[index] = amount;
    //}

    public void SetData()
    {
        foreach (var currencyType in currencyTypes)
        {
            int index = currencyTypes.IndexOf((int)currencyType);

            var currencyModule = InventoryManager.Instance != null ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>() : null;
            currencyModule?.AddCurrency((CurrencyType)currencyType, currencyValues[index]);

            //if (currencyTypeToKey.ContainsKey((CurrencyType)currencyType))
            //{
            //    InventoryManager.Instance.AddItem(currencyTypeToKey[(CurrencyType)currencyType], currencyValues[index]);
            //}
            //else
            //{
            //    var currencyModule = InventoryManager.Instance != null? InventoryManager.Instance.GetModule<CurrencyInventoryModule>(): null;
            //    currencyModule?.AddCurrency((CurrencyType)currencyType, currencyValues[index]);
            //}
        }

        for (int i = 0; i < itemId.Count; i++)
        {
            InventoryManager.Instance.AddItem(itemId[i], itemValue[i]);
        }
    }

    //public void SetOtherCurrencyData()
    //{
    //    var currencyModule = InventoryManager.Instance != null
    //? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
    //: null;
    //    currencyModule?.AddCurrency(CurrencyType.Exp, finalExp);
    //}
}
