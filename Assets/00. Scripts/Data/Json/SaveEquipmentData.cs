using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
//나중엔 해당 클래스를 매니저에서 관리하지만 현재는 테스트를 위해 해당 데이터에 Singleton 적용
public class SaveEquipmentData
{
    //정해야할 데이터 플레이어가 장착한 아이템 정보들, 각 아이템 갯수, 아이템 해금 정보, 패밀리어랑, 패밀리어 주문서 양
    [Header("저장된 장착 장비")]
    public int weaponId =-1;
    public int helmetId;
    public int gloveId;
    public int armorId;
    public int bootsId;
    [Header("저장된 해금 장비")]
    //해금된 아이템 키
    public List<int> unLockEquipmentKey;
    //해금된 아이템 갯수
    public List<int> unLockEquipmentValue;

    //해당 값은 Dictionary이므로 Json에 저장되지 않음, 다른 스크립트에서 참고하거나 풀어서 위 리스트에 저장용
    public Dictionary<int, int> unlockEquipmentDict = new Dictionary<int, int>();


    //데이터가 없을 시 데이터 초기 세팅
    //기본 장비 장착 및 기본 장비 해금 후 갯수 0으로 세팅
    public SaveEquipmentData()
    {  }

    //만약 데이터를 받아오지 않았다면 무기 id가 -1이므로 초기 세팅 진행
    public void InitPlayerEquipmentData()
    {
        if (weaponId <= 0)
        {
            //Debug.Log("$[TestSavePlayerEquipmentData] 테이블이 비었다");
            weaponId = FirstDictionaryKey(DataManager.Instance.EquipWeaponDict);
            helmetId = FirstDictionaryKey(DataManager.Instance.EquipHelmetDict);
            gloveId = FirstDictionaryKey(DataManager.Instance.EquipGloveDict);
            armorId = FirstDictionaryKey(DataManager.Instance.EquipArmorDict);
            bootsId = FirstDictionaryKey(DataManager.Instance.EquipBootsDict);

            unLockEquipmentKey = new List<int>();
            unLockEquipmentKey.Add(weaponId);
            unLockEquipmentKey.Add(helmetId);
            unLockEquipmentKey.Add(gloveId);
            unLockEquipmentKey.Add(armorId);
            unLockEquipmentKey.Add(bootsId);
            unLockEquipmentValue = new List<int>();
            for (int i = 0; i < unLockEquipmentKey.Count; i++)
            {
                //처음 시작시 기본 장비 갯수는 0으로 시작
                unLockEquipmentValue.Add(0);
                //해당 리스트 기반으로 딕셔너리 제작
                unlockEquipmentDict[unLockEquipmentKey[i]] = unLockEquipmentValue[i];
            }
        }

        SetDict();
    }

    public void SetDict()
    {
        for (int i = 0; i < unLockEquipmentKey.Count; i++)
        {
            //해당 리스트 기반으로 딕셔너리 제작
            unlockEquipmentDict[unLockEquipmentKey[i]] = unLockEquipmentValue[i];
            //Debug.Log($"[TestSavePlayerEquipmentData] 아이디 : {unLockEquipmentKey[i]} 값 : {unLockEquipmentValue[i]}");
        }

    }

    //종료 전에 지금까지 참조한 데이터들 딕셔너리 기반으로 다시 리스트 저장
    //딕셔너리는 얕은 참조 이므로 주소값만 복사하여 자동을 동기화가 이루어짐
    public void SaveBeforeQuit(int weaponId, int helmetId, int gloveId, int armorId, int bootsId)
    {
        //해당 값이 0이면 사실상 게임을 껐다가 바로 끄는 경우 이다. 이러할 경우 데이터를 저장하지 않는다(이전 데이터 사용)
        if (weaponId == 0)
            return;
        this.weaponId= weaponId;
        this.helmetId = helmetId;
        this.gloveId=gloveId;
        this.armorId=armorId;
        this.bootsId = bootsId;
        unLockEquipmentKey.Clear();
        unLockEquipmentKey = unlockEquipmentDict.Keys.ToList<int>();
        unLockEquipmentValue.Clear();
        unLockEquipmentValue = unlockEquipmentDict.Values.ToList<int>();
    }

    public void SaveEquipment(InventoryItemContext context, BigDouble amount)
    {
        //장비는 10의 자리 숫자가 1
        if ((int)context.ItemType/10 !=1)
            return;
        double count = amount.ToDouble();
        int equipmentItemCount = Convert.ToInt32(count);
        unlockEquipmentDict[context.ItemId] = equipmentItemCount;
    }

    public int FirstDictionaryKey<T>(Dictionary<int,T> table)
    {
        if (table == null)
        {
            Debug.Log("[TestSavePlayerEquipmentData] 테이블이 비었다");
            return 0;
        }
        if (table.Count == 0)
        {
            Debug.Log("[TestSavePlayerEquipmentData] 테이블 크기가 0이다");
            return 0;
        }
        List<int> keyList = table.Keys.ToList<int>();
        keyList.Sort();
        return keyList[0];
    }
}
