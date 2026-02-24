using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentHandler : MonoBehaviour
{
    public PlayerEquipment playerEquipment;
    public PlayerInventory playerInventory;

    public Button autoMerge;
    public Button autoEquip;
    ////종류별 장비 데이터를 참조하기 위한 딕셔너리
    //public Dictionary<EquipmentType, Dictionary<int, TableBase>> equipmentTableDict;



    IEnumerator Start()
    {
        yield return new WaitUntil(()=>DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
    }
    //시작 시 플레이어 착용하던 장비 아이템을 다시 장착 및 인벤토리 정보 불러 코드
    public void SetMyEquipOnStart(int weaponId, int helmetId, int glovesId, int armorId, int bootsId, Dictionary<int,int> equipCountDict)
    {
        //나중에 플레이어 관련 매니저에서 정보 불러와 해당 장비 장착
        playerEquipment.equipmentHandler = this;
        playerEquipment.OnEqipItem(weaponId);
        playerEquipment.OnEqipItem(helmetId);
        playerEquipment.OnEqipItem(glovesId);
        playerEquipment.OnEqipItem(armorId);
        playerEquipment.OnEqipItem(bootsId);

        //인벤토리 정보 불러옴
        playerInventory.equipmentHandler=this;
        playerInventory.SetMyEquipmentCountDictionary(equipCountDict);
        playerInventory.SetMyEquipmentInventory();

        autoMerge.onClick.AddListener(playerInventory.AutoMerge);
        autoEquip.onClick.AddListener(AutoEquip);


        //autoMerge.gameObject.SetActive(false);
        //autoEquip.gameObject.SetActive(false);

        autoMerge.interactable=false;
        autoEquip.interactable=false;

        CheckAutoEquip();
        playerInventory.CheckAutoMerge();
    }

    //해당 작업에 필요한 과정 <- 아래 과정은 장비마다 반복
    //0. 해금된 것들 중 최상위 능력치를 가진 장비를 찾는다.
    //1. 찾은 장비와 중복되는 원래 장비를 뺀다(같은 것 뺴는거 아님)
    //2. 장비를 빼면서 상승했던 스탯을 감소시킨다.
    //3. 더 상위의 장비로 갈아낀다
    //4. 장비를 갈아끼면서 상승되는 스탯을 증가시킨다
    //5. 스탯 증가 UI를 출력한다

    public void AutoEquip()
    {
        int itemId = 0;
        foreach (EquipmentType t in Enum.GetValues(typeof(EquipmentType)))
        {
            itemId = playerInventory.FindBestEquipment(t);
            if (itemId == playerEquipment.ReturnItemNum(t))
            {
                continue;
            }
            else
            {
                playerEquipment.OnEqipItem(itemId);
                //장착 이펙트 발생 코드
                //효과음 발생 코드
                //최종 전투력 수치 표시 코드
            }

        }
        autoEquip.interactable=false;
        //autoEquip.gameObject.SetActive(false);//버튼 비활성화 코드 <- 해당 버튼은 이후 추가적인 최고 등급 아이템을 얻기 전에는 풀리지 않음
    }
    public void CheckAutoEquip()
    {
        //if(autoEquip.gameObject.activeSelf)
        //    return;
        if(autoEquip.interactable)
            return;

        int itemId = 0;
        foreach (EquipmentType t in Enum.GetValues(typeof(EquipmentType)))
        {
            itemId = playerInventory.FindBestEquipment(t);
            if (itemId == playerEquipment.ReturnItemNum(t))
            {
                continue;
            }
            autoEquip.interactable = true;
            //autoEquip.gameObject.SetActive(true);
            break;
        }
    }
}
