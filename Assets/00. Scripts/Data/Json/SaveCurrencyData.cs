using AYellowpaper.SerializedCollections;
using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveCurrencyData
{
    //골드, 크리스탈을 저장하는 cs
    [Header("저장할 재화")]
    //public BigDouble gold;
    //public BigDouble crystal;

    //현재 json 상에 딕셔너리가 저장이 안되어 사싱상 currencyTypes 의 인덱스를 키 값으로 currencyValue를 저장
    //enum값의 정수 값
    public List<int> currencyTypes;
    //각 타입별 갯수
    public List<BigDouble> currencyValues;

    //[Header("재화 주소")]
    //int goldIndex=0;
    //int crystalIndex=0;

    //public SerializedDictionary<CurrencyType, int> currencyTypeToValue= new SerializedDictionary<CurrencyType, int>();
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

            //타입과 값을 같은 인덱스에 배치
            foreach (CurrencyType currencyType in Enum.GetValues(typeof(CurrencyType)))
            {
                currencyTypes.Add((int)currencyType);
                currencyValues.Add(BigDouble.Zero);
            }
        }


        //이후에 enum값이 추가될 가능성 대비
        if (currencyTypes.Count < Enum.GetValues(typeof(CurrencyType)).Length)
        {
            foreach (CurrencyType currencyType in Enum.GetValues(typeof(CurrencyType)))
            {
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
        if (!currencyTypes.Contains((int)context.ItemType))
            return;
        //값을 찾을 인덱스
        int index = currencyTypes.IndexOf((int)context.ItemType);
        Debug.Log($"[InventoryManager] {context.ItemType}의 인덱스 : {index} 증가량 {amount}");
        currencyValues[index] = amount;
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

    //이걸 OnApplicationQuit에서 사용할 거임
    public void SaveBeforeQuit(CurrencyType currencyType, BigDouble amount)
    {
        //값을 찾을 인덱스
        int index = currencyTypes.IndexOf((int)currencyType);
        Debug.Log($"[InventoryManager] {currencyType}의 인덱스 : {index} 증가량 {amount}");
        currencyValues[index] = amount;
    }

    public void SetData()
    {
        foreach (var currencyType in currencyTypes)
        {
            int index = currencyTypes.IndexOf((int)currencyType);
            if (currencyTypeToKey.ContainsKey((CurrencyType)currencyType))
            {
                InventoryManager.Instance.AddItem(currencyTypeToKey[(CurrencyType)currencyType], currencyValues[index]);
            }
            else
            {
                var currencyModule = InventoryManager.Instance != null
? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
: null;
                currencyModule?.AddCurrency((CurrencyType)currencyType, currencyValues[index]);
            }
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
