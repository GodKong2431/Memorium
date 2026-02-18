using System.Collections;
using UnityEngine;

public class TestPlayerDataManager : Singleton<TestPlayerDataManager>
{
    [SerializeField] TestSavePlayerEquipmentData testSaveData;
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
        testSaveData = JSONService.Load<TestSavePlayerEquipmentData>();
        testSaveData.InitPlayerEquipmentData();
        //불러온 데이터 플레이어 장착 및 데이터 세팅
        equipmentHandler.SetMyEquipOnStart(testSaveData.weaponId, testSaveData.helmetId, testSaveData.gloveId, testSaveData.armorId, testSaveData.bootsId, testSaveData.unlockEquipmentDict);
    }

    protected override void OnApplicationQuit()
    {
        base.OnApplicationQuit();
        PlayerEquipment playerEquipment = equipmentHandler.playerEquipment;
        testSaveData.SaveBeforeQuit(playerEquipment.weapon.ID, playerEquipment.helmet.ID, playerEquipment.glove.ID, playerEquipment.armor.ID, playerEquipment.boots.ID);
        JSONService.Save(testSaveData);
    }

}
