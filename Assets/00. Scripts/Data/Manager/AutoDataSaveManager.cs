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

    private IEnumerator Start()
    {
        //CurrencyData, SkillData, PixieData
        yield return new WaitUntil(() => InventoryManager.Instance != null);
        yield return new WaitUntil(() => InventoryManager.Instance.DataLoad);
        currencyData = InventoryManager.Instance.saveCurrencyData;
        skillData = InventoryManager.Instance.saveSkillData;
        pixieData = InventoryManager.Instance.savePixieData;

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
        if(gachaData.IsDirty && gachaData!=null) saveTask.Add(AutoSaveTaskStart(gachaData));
        if(skillData.IsDirty && skillData!=null) saveTask.Add(AutoSaveTaskStart(skillData));
        if(abilityStoneData.IsDirty && abilityStoneData != null) saveTask.Add(AutoSaveTaskStart(abilityStoneData));
        if (pixieData.IsDirty && pixieData != null) saveTask.Add(AutoSaveTaskStart(pixieData));

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
