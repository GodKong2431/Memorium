using System.Collections;
using UnityEngine;

public class TestPlayerDataManager : Singleton<TestPlayerDataManager>
{
    [SerializeField] SaveEquipmentData testSaveData;
    [SerializeField] EquipmentHandler equipmentHandler;

    [Header("테스트용 플레이어 스탯")]
    public int playerAttack;
    public float playerAttackSpeed;
    public float playerDefense;
    public float playerMagicDefense;
    public int playerHp;
    public float playerMoveSpeed;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        //시작 시 데이터 불러옴
        testSaveData = JSONService.Load<SaveEquipmentData>();
        testSaveData.InitPlayerEquipmentData();
        //불러온 데이터 플레이어 장착 및 데이터 세팅
        //equipmentHandler.SetMyEquipOnStart(testSaveData.weaponId, testSaveData.helmetId, testSaveData.gloveId, testSaveData.armorId, testSaveData.bootsId, testSaveData.unlockEquipmentValueDict);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        if (equipmentHandler == null)
            return;
        if (!equipmentHandler.TryGetPlayerEquipment(out var playerEquipment))
            return;
        if (testSaveData == null)
            return;

        testSaveData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
        JSONService.Save(testSaveData);
    }

}
