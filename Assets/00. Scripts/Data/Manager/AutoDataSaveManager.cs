using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AutoDataSaveManager : Singleton<AutoDataSaveManager>
{
    [SerializeField] private float autoSaveDelay = 300f;

    SaveCurrencyData currencyData;
    SaveEquipmentData equipmentData;
    SaveGachaData gachaData;
    SavePlayerData playerData;
    SaveQuestData  questData;
    SaveSkillData skillData;
    SaveStageData stageData;

    private IEnumerator Start()
    {
        //CurrencyData
        yield return new WaitUntil(() => InventoryManager.Instance != null);
        yield return new WaitUntil(() => InventoryManager.Instance.DataLoad);
        currencyData = InventoryManager.Instance.saveCurrencyData;

        //PlayerData, equipmentData
        yield return new WaitUntil(() => CharacterStatManager.Instance != null);
        yield return new WaitUntil(() => CharacterStatManager.Instance.TableLoad);
        playerData = CharacterStatManager.Instance.savePlayerData;
        equipmentData = CharacterStatManager.Instance.saveEquipmentData;

        //QuestData
        yield return new WaitUntil(() => QuestManager.Instance != null);
        yield return new WaitUntil(() => QuestManager.Instance.DataLoad);
        questData =QuestManager.Instance.saveQuestData;

        //StageData
        yield return new WaitUntil(() => StageManager.Instance != null);
        yield return new WaitUntil(() => StageManager.Instance.DataLoad);
        stageData = StageManager.Instance.saveStageData;

        //스킬 데이터, 가챠 데이터


        StartCoroutine(AutoSaveCoroutine());
    }
    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return CoroutineManager.waitForSeconds(autoSaveDelay);
            _ = AutoSaveTask();
        }
    }

    private async Task AutoSaveTask()
    {
        List<Task> saveTask = new List<Task>();
        //스테이지 매니저 데이터 저장 여부 체크
        //if (StageManager.Instance.saveStageData.IsDirty && StageManager.Instance.DataLoad) saveTask.Add(StageManager.Instance.AutoSaveTask());
        //if (InventoryManager.Instance.saveCurrencyData.IsDirty && InventoryManager.Instance.DataLoad) saveTask.Add(InventoryManager.Instance.AutoSaveTask());
        //if (CharacterStatManager.Instance.saveEquipmentData.IsDirty && CharacterStatManager.Instance.TableLoad) saveTask.Add(CharacterStatManager.Instance.AutoSaveTaskOnEquipmentData());
        //if (CharacterStatManager.Instance.savePlayerData.IsDirty && CharacterStatManager.Instance.TableLoad) saveTask.Add(CharacterStatManager.Instance.AutoSaveTaskOnPlayerData());
        //if (QuestManager.Instance.saveQuestData.IsDirty && QuestManager.Instance.DataLoad) saveTask.Add(QuestManager.Instance.AutoSaveTask());
        if (stageData.IsDirty && stageData!=null) saveTask.Add(AutoSaveTaskStart(stageData));
        if (currencyData.IsDirty && currencyData!=null) saveTask.Add(AutoSaveTaskStart(currencyData));
        if (equipmentData.IsDirty && equipmentData != null) saveTask.Add(AutoSaveTaskStart(equipmentData));
        if (playerData.IsDirty && playerData!=null) saveTask.Add(AutoSaveTaskStart(playerData));
        if (questData.IsDirty && questData!=null) saveTask.Add(AutoSaveTaskStart(questData));


        if (saveTask.Count > 0)
        {
            Debug.Log($"[AutoDataSaveManager] {saveTask.Count}개 데이터 변경사항 저장 완료.");
            await Task.WhenAll(saveTask);
        }
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            _ = AutoSaveTask();
        }
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        _ = AutoSaveTask();
    }

    public async Task AutoSaveTaskStart<T>(T data) where T : ISaveData
    {
        Debug.Log($"[StageManager] {typeof(T).Name} 확인 및 데이터 저장");
        await JSONService.SaveFileOnAsync(data);
        data.ClearDirty();
    }
}
