using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    //게임 종료 시 혹은 특정 상황에서 아이디별 보유 갯수와 해금 여부를 저장할 코드
    //처음에 csv에서 데이터를 받아올 코드

    //해당 아이템의 해금 여부 <- 해금 여부는 아래 리스트만으로 가능
    //public Dictionary<int, bool> equipmentUnlock;
    //해당 아이템의 소유 개수 <- 해당 값에는 해금된 아이템만 아이디 값으로 받는다
    public Dictionary<int, int> equipmentCount;
    public List<int> myEquipmentListKeys = new List<int>();
    public List<int> myEquipmentCountKeys = new List<int>();

    public List<int> allEquipmentListKeys;

    [SerializeField] private Transform[] uIPage;

    public EquipmentHandler equipmentHandler;

    [SerializeField] Text testInventory;
    Dictionary<EquipmentType, int> mergeItemByType = new Dictionary<EquipmentType, int>();

    Dictionary<int, EquipmentSlotComponent> allEquipmentComponents = new Dictionary<int, EquipmentSlotComponent>();

    //사실상 테이블 값 전체를 가지고 와야할 듯 함
    //가져온 테이블을 순서대로 배치 후 가져오는 시점에서 해당 아이디 값이 인벤토리 내에 존재하는지 확인 -> 색 변환
    //없으면 회색으로 있으면 각 등급에 맞도록 색을 바꾸자

    //
    public GameObject prefabEquipTierGroup;

    public List<int> finalEquipment;

    public void SetMyEquipmentInventory()
    {
        allEquipmentListKeys = DataManager.Instance.EquipListDict.Keys.ToList();
        allEquipmentListKeys.Sort();

        //한 번에 넣을 값 : 현재는 일반, 희귀, 레어, 전설, 신화로 있어 총 5개
        int count = prefabEquipTierGroup.GetComponent<EquipmentSlotContainer>().slot.Count;
        for (int i = 0; i < allEquipmentListKeys.Count; i+=5)
        {
            //1 = 무기 2 = 투구 3 = 장갑 4 = 갑옷 5 = 신발
            int equipType = allEquipmentListKeys[i] / 10000 % 10;
            GameObject equipmentUIGroup = Instantiate(prefabEquipTierGroup, uIPage[equipType-1]);
            EquipmentSlotContainer slotContainer = equipmentUIGroup.GetComponent<EquipmentSlotContainer>();
            for (int j = i; j < i + 5; j++)
            {
                int index = allEquipmentListKeys[j];
                if (string.IsNullOrEmpty(slotContainer.gradeText.text))
                {
                    slotContainer.gradeText.text = DataManager.Instance.EquipListDict[index].grade+"";
                }
                allEquipmentComponents[index] = slotContainer.slot[j-i];
                //slotContainer.slot[index].slotImage =  <= 나중에 이미지 넣을때 사용
                if (equipmentCount.ContainsKey(index))
                {
                    slotContainer.slot[j - i].ownerShipImage.gameObject.SetActive(false);
                    slotContainer.slot[j - i].equipmentCountSlider.value = equipmentCount[index];
                    //slotContainer.slot[j - i].equipmentCountText.text = equipmentCount[index] + " / 3";

                    string itemCount;
                    if (equipmentCount[index] > 99)
                        itemCount = 99 + "+";
                    else itemCount = equipmentCount[index].ToString();
                    slotContainer.slot[j - i].equipmentCountText.text = itemCount + " / 3";
                }
            }

        }

    }

    public void FindFinalEquipment()
    {

        //int equipmentTier = 0;
        //int finalEquipmentIndex = 0;

        int count = Enum.GetNames(typeof(EquipmentType)).Length;
        int[] equipmentTierArr = new int[count];
        int[] finalEquipmentIndexArr = new int[count];
        //int finalEquipmentIndex = 0;

        foreach (int index in allEquipmentListKeys)
        {
            int itemTypeIndex = (int)DataManager.Instance.EquipListDict[index].equipmentType - (int)EquipmentType.Weapon;
            if (DataManager.Instance.EquipListDict[index].equipmentTier > equipmentTierArr[itemTypeIndex])
            {
                equipmentTierArr[itemTypeIndex] = DataManager.Instance.EquipListDict[index].equipmentTier;
                finalEquipmentIndexArr[itemTypeIndex] = index;
            }
        }

        foreach (var index in finalEquipmentIndexArr)
        {
            finalEquipment.Add(index);
        }

        //foreach (EquipmentType t in Enum.GetValues(typeof(EquipmentType)))
        //{
        //    foreach (var dict in DataManager.Instance.EquipListDict)
        //    {
        //        if (dict.Value.equipmentType == t)
        //        {
        //            if (dict.Value.equipmentTier > equipmentTier)
        //            {
        //                finalEquipmentIndex = dict.Value.ID;
        //            }
        //        }
        //        finalEquipment.Add(finalEquipmentIndex);
        //    }
        //}
    }
    public void SetMyEquipmentCountDictionary(Dictionary<int, int> dict)
    {
        Debug.Log($"[PlayerInventory] 아이디와 갯수 가져오기");
        equipmentCount = dict;

        //테스트용 출력
        foreach (var k in dict)
        {
            Debug.Log($"[PlayerInventory] 아이디 : {k.Key} 갯수 : {k.Value}");
        }
    }

    //각 부위별 보유중인 장비의 가장 최상의 부위 반환
    public int FindBestEquipment(EquipmentType equipmentType)
    {
        int itemId = 0;

        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                myEquipmentListKeys = DataManager.Instance.EquipWeaponDict.Keys.ToList<int>();
                break;
            case EquipmentType.Helmet:
                myEquipmentListKeys = DataManager.Instance.EquipHelmetDict.Keys.ToList<int>();
                break;
            case EquipmentType.Glove:
                myEquipmentListKeys = DataManager.Instance.EquipGloveDict.Keys.ToList<int>();
                break;
            case EquipmentType.Armor:
                myEquipmentListKeys = DataManager.Instance.EquipArmorDict.Keys.ToList<int>();
                break;
            case EquipmentType.Boots:
                myEquipmentListKeys = DataManager.Instance.EquipBootsDict.Keys.ToList<int>();
                break;
        }


        myEquipmentListKeys.Sort();
        myEquipmentListKeys.Reverse();

        foreach (int key in myEquipmentListKeys)
        {
            if (equipmentCount.ContainsKey(key))
            {
                itemId = key;
                break;
            }
        }
        myEquipmentListKeys.Clear();
        return itemId;
    }


    public void AutoMerge()
    {
        //모든 장비 테이블의 키를 가져와서 저장 <- 합성 시 다음 테이블 키의 값을 올리기 위함 
        //if (myEquipmentCountKeys.Count <= 0)
        //{
        //    myEquipmentListKeys = DataManager.Instance.EquipListDict.Keys.ToList<int>();
        //    myEquipmentListKeys.Sort();
        //}
        //현재 소유중인 아이템의 키값들 전부 가져옴
        myEquipmentCountKeys.Clear();
        myEquipmentCountKeys = equipmentCount.Keys.ToList<int>();
        myEquipmentCountKeys.Sort();

        //Dictionary<EquipmentType, int> mergeItemByType= new Dictionary<EquipmentType, int>();

        mergeItemByType.Clear();

        //생각해보니 해금 안된 것들도 합성 과정에서 해금 되는 경우가 있어서 모든 itemListKeys 봐야 겠다
        foreach (int key in allEquipmentListKeys)
        {
            //해당 키값을 가지고 있지 않으면(해금하지 않았으면 넘긴다)
            if (!equipmentCount.ContainsKey(key))
                continue;
            //최종 티어 장비면 다음으로 넘긴다
            if (finalEquipment.Contains(key))
                continue ;
            if (equipmentCount[key] >= 3)
            {
                //키값의 인덱스를 찾고 1을 추가하여 다음 단계의 아이템 인덱스를 가지고 온다
                Debug.Log($"[PlayerInventory] 아이템 합성 : {key} 인덱스 {allEquipmentListKeys.IndexOf(key)}");
                int nextIndex = allEquipmentListKeys[allEquipmentListKeys.IndexOf(key) + 1];
                Debug.Log("[PlayerInventory] 아이템 합성 성공");
                //만약 다음 단계의 아이템이 락 상태면 해금 시킨다
                //if (!equipmentCount.ContainsKey(nextIndex))
                //{
                //    equipmentCount[nextIndex] = 0;
                //    //equipmentUnlock[nextIndex] = true;
                //}
                //다음 단계의 아이템 인덱스의 갯수를 증가시킨다
                //equipmentCount[nextIndex] += equipmentCount[key] / 3;
                //원래 단계의 아이템은 3개씩 나눈 나머지를 가진다
                //equipmentCount[key] %= 3;

                int plusCount = DivideEquipment(key, 3);
                IncreaseEquipment(nextIndex, plusCount);

                //합성 이펙트 출력
                //합성 사운드 출력

                mergeItemByType[DataManager.Instance.EquipListDict[nextIndex].equipmentType] = nextIndex;
            }
        }

        //여기서 타입에따라 가장 높은 티어 장비의 합성 결과 인덱스 불러오기 가능
        foreach (var dic in mergeItemByType)
        {
            Debug.Log($"아이템 타입 : {dic.Key} 아이템 번호 : {dic.Value}");
        }
        //ShowMyInventory();
        //equipmentHandler.autoMerge.gameObject.SetActive(false);
        equipmentHandler.autoMerge.interactable = false;
        //일괄합성 버튼 비활성화 <- 다음에 아이템 얻을 경우마다 체크 후 출력
        equipmentHandler.CheckAutoEquip();
    }

    public void ShowMyInventory()
    {
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var dic in equipmentCount)
        {
            stringBuilder.AppendLine("ID : "+dic.Key + " 이름 : "+ DataManager.Instance.EquipListDict[dic.Key] +" 갯수 : "+dic.Value);
        }
        testInventory.text =stringBuilder.ToString();
    }

    public void CheckAutoMerge()
    {
        //if(equipmentHandler.autoMerge.gameObject.activeSelf)
        //    return;

        if(equipmentHandler.autoMerge.interactable)
            return;

        foreach (var count in equipmentCount)
        {
            if (finalEquipment.Contains(count.Key))
            {
                Debug.Log($"[PlayerInventory] 최종 등급 장비는 합성이 불가능 합니다.");
                continue;
            }
            if ((count.Value>=3))
            {
                //equipmentHandler.autoMerge.gameObject.SetActive(true);
                equipmentHandler.autoMerge.interactable = true;
                break;
            }
        }
    }

    public void IncreaseEquipment(int itemNum, int count)
    {
        if (equipmentCount.ContainsKey(itemNum))
        {
            equipmentCount[itemNum] += count;
            allEquipmentComponents[itemNum].equipmentCountSlider.value = equipmentCount[itemNum];
            string itemCount;
            if (equipmentCount[itemNum] > 99)
                itemCount = 99 + "+";
            else itemCount = equipmentCount[itemNum].ToString();
            //allEquipmentComponents[itemNum].equipmentCountText.text = equipmentCount[itemNum] + " / 3";
            allEquipmentComponents[itemNum].equipmentCountText.text = itemCount + " / 3";
        }
        else
        {
            equipmentCount[itemNum] = count;
            allEquipmentComponents[itemNum].ownerShipImage.SetActive(false);
            allEquipmentComponents[itemNum].equipmentCountSlider.value = count;
            //allEquipmentComponents[itemNum].equipmentCountText.text = count + " / 3";
            string itemCount;
            if (equipmentCount[itemNum] > 99)
                itemCount = 99 + "+";
            else itemCount = equipmentCount[itemNum].ToString();
            //allEquipmentComponents[itemNum].equipmentCountText.text = equipmentCount[itemNum] + " / 3";
            allEquipmentComponents[itemNum].equipmentCountText.text = itemCount + " / 3";
        }

        CheckAutoMerge();
        equipmentHandler.CheckAutoEquip();
    }
    public void DecreaseEquipment(int itemNum, int count)
    {
        if (!equipmentCount.ContainsKey(itemNum))
        {
            Debug.Log($"[PlayerInventory] 해당 아이템 [{itemNum}]을 보유하고 있지 않습니다");
            return;
        }
        equipmentCount[itemNum] -= count;
        allEquipmentComponents[itemNum].equipmentCountSlider.value = equipmentCount[itemNum];
        allEquipmentComponents[itemNum].equipmentCountText.text = equipmentCount[itemNum] + " / 3";
    }

    public int DivideEquipment(int itemNum, int divid)
    {
        if (!equipmentCount.ContainsKey(itemNum))
        {
            Debug.Log($"[PlayerInventory] 해당 아이템 [{itemNum}]을 보유하고 있지 않습니다");
            return -1;
        }
        int returnCount = equipmentCount[itemNum] / divid;
        equipmentCount[itemNum] = equipmentCount[itemNum] % divid;
        allEquipmentComponents[itemNum].equipmentCountSlider.value = equipmentCount[itemNum];
        allEquipmentComponents[itemNum].equipmentCountText.text = equipmentCount[itemNum] + " / 3";
        return returnCount;
    }
}
