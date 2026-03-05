using NUnit.Framework;
using System.Collections;
using UnityEngine;

public class SaveDataLoadManager_JSON : Singleton<SaveDataLoadManager_JSON>
{
    [SerializeField] TestSavePlayerEquipmentData saveEquipmentData;
    [SerializeField] SavePlayerData savePlayerData;
    [SerializeField] SaveCurrencyData saveCurrencyData;
    [SerializeField] SaveSkillData saveSkillData;
    [SerializeField] SaveStageData saveStageData;
    [SerializeField] SaveQuestData saveQuestData;

    public bool DataLoad=false;
    public bool DataSave = false;
    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        yield return new WaitUntil(() => InventoryManager.Instance != null);
        saveCurrencyData = JSONService.Load<SaveCurrencyData>();
        saveCurrencyData.InitCurrencyData();
        saveCurrencyData.SetData();

        DataLoad = true;
    }
    protected override void OnApplicationQuit()
    {
        if (!DataLoad)
            return;
        //saveCurrencyData.SaveBeforeQuit();
        JSONService.Save(saveCurrencyData);
    }
}
