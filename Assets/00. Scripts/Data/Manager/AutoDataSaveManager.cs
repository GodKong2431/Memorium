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
    SaveAbilityStoneData abilityStoneData;
    SavePixieData pixieData;
    SaveGemData gemData;

    private bool onSave=false;

    private IEnumerator Start()
    {
        //CurrencyData, SkillData, PixieData
        yield return new WaitUntil(() => InventoryManager.Instance != null);
        yield return new WaitUntil(() => InventoryManager.Instance.DataLoad);
        currencyData = InventoryManager.Instance.saveCurrencyData;
        skillData = InventoryManager.Instance.saveSkillData;
        pixieData = InventoryManager.Instance.savePixieData;
        gemData = InventoryManager.Instance.saveGemData;

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

        //가챠 데이터
        yield return new WaitUntil(() => GachaManager.Instance != null);
        yield return new WaitUntil(() => GachaManager.Instance.DataLoad);
        gachaData = GachaManager.Instance.saveGachaData;

        //어빌리티 스톤 데이터
        yield return new WaitUntil(() => AbilityStoneManager.Instance != null);
        yield return new WaitUntil(() => AbilityStoneManager.Instance.LoadStone);
        abilityStoneData = AbilityStoneManager.Instance.saveAbilityStoneData;

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

    public async Task AutoSaveTask()
    {

        if(onSave)
            return;
        onSave = true;

        List<Task> saveTask = new List<Task>();
        //스테이지 매니저 데이터 저장 여부 체크
        if (stageData!= null&&stageData.IsDirty) saveTask.Add(AutoSaveTaskStart(stageData));
        if (currencyData != null&&currencyData.IsDirty) saveTask.Add(AutoSaveTaskStart(currencyData));
        if (equipmentData != null&& equipmentData.IsDirty ) saveTask.Add(AutoSaveTaskStart(equipmentData));
        if (playerData != null&&playerData.IsDirty) saveTask.Add(AutoSaveTaskStart(playerData));
        if (questData != null&&questData.IsDirty ) saveTask.Add(AutoSaveTaskStart(questData));
        if(gachaData != null&&gachaData.IsDirty) saveTask.Add(AutoSaveTaskStart(gachaData));
        if(skillData != null&&skillData.IsDirty) saveTask.Add(AutoSaveTaskStart(skillData));
        if(abilityStoneData != null&&abilityStoneData.IsDirty ) saveTask.Add(AutoSaveTaskStart(abilityStoneData));
        if (pixieData != null&&pixieData.IsDirty) saveTask.Add(AutoSaveTaskStart(pixieData));
        if(gemData != null&&gemData.IsDirty) saveTask.Add(AutoSaveTaskStart(gemData));

        if (saveTask.Count > 0)
        {
            Debug.Log($"[AutoDataSaveManager] {saveTask.Count}개 데이터 변경사항 저장 완료.");
            await Task.WhenAll(saveTask);
        }
            onSave = false;
    }

    public void SaveData()
    {
        if (stageData != null&&stageData.IsDirty) SaveData(stageData);
        if (currencyData != null && currencyData.IsDirty ) SaveData(currencyData);
        if (equipmentData != null&& equipmentData.IsDirty) SaveData(equipmentData);
        if (playerData != null&& playerData.IsDirty) SaveData(playerData);
        if (questData != null&& questData.IsDirty) SaveData(questData);
        if (gachaData != null&& gachaData.IsDirty) SaveData(gachaData);
        if (skillData != null&&skillData.IsDirty) SaveData(skillData);
        if (abilityStoneData != null&&abilityStoneData.IsDirty) SaveData(abilityStoneData);
        if (pixieData != null && pixieData.IsDirty) SaveData(pixieData);
        if (gemData != null&& gemData.IsDirty) SaveData(gemData);
    }

    private void OnApplicationPause(bool pause)
    {
        if (pause)
        {
            //_ = AutoSaveTask();
            SaveData();
        }
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        //_ = AutoSaveTask();
        SaveData();
    }

    public async Task AutoSaveTaskStart<T>(T data) where T : ISaveData
    {
        Debug.Log($"[StageManager] {typeof(T).Name} 확인 및 데이터 저장");
        await JSONService.SaveFileOnAsync(data);
        data.ClearDirty();
    }

    public void SaveData<T>(T data) where T : ISaveData
    {
        //Debug.Log($"[StageManager] {typeof(T).Name} 확인 및 데이터 저장");
        JSONService.Save(data);
        data.ClearDirty();
    }
}
