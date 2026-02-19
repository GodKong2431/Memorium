using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInventory : MonoBehaviour
{
    //게임 종료 시 혹은 특정 상황에서 아이디별 보유 갯수와 해금 여부를 저장할 코드
    //처음에 csv에서 데이터를 받아올 코드

    //해당 아이템의 해금 여부 <- 해금 여부는 아래 리스트만으로 가능
    //public Dictionary<int, bool> equipmentUnlock;
    //해당 아이템의 소유 개수 <- 해당 값에는 해금된 아이템만 아이디 값으로 받는다
    public Dictionary<int, int> equipmentCount;
    public List<int> itemListKeys = new List<int>();
    public List<int> myEquipmentCountKeys = new List<int>();

    [SerializeField] Text testInventory;
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
                itemListKeys = DataManager.Instance.EquipWeaponDict.Keys.ToList<int>();
                break;
            case EquipmentType.Helmet:
                itemListKeys = DataManager.Instance.EquipHelmetDict.Keys.ToList<int>();
                break;
            case EquipmentType.Gloves:
                itemListKeys = DataManager.Instance.EquipGloveDict.Keys.ToList<int>();
                break;
            case EquipmentType.Armor:
                itemListKeys = DataManager.Instance.EquipArmorDict.Keys.ToList<int>();
                break;
            case EquipmentType.Boots:
                itemListKeys = DataManager.Instance.EquipBootsDict.Keys.ToList<int>();
                break;
        }


        itemListKeys.Sort();
        itemListKeys.Reverse();

        foreach (int key in itemListKeys)
        {
            if (equipmentCount.ContainsKey(key))
            {
                itemId = key;
                break;
            }
        }
        itemListKeys.Clear();
        return itemId;
    }


    public void AutoMerge()
    {
        //모든 장비 테이블의 키를 가져와서 저장 <- 합성 시 다음 테이블 키의 값을 올리기 위함 
        if (itemListKeys.Count <= 0)
        {
            itemListKeys = DataManager.Instance.EquipListDict.Keys.ToList<int>();
            itemListKeys.Sort();
        }
        //현재 소유중인 아이템의 키값들 전부 가져옴
        myEquipmentCountKeys.Clear();
        myEquipmentCountKeys = equipmentCount.Keys.ToList<int>();
        myEquipmentCountKeys.Sort();

        //생각해보니 해금 안된 것들도 합성 과정에서 해금 되는 경우가 있어서 모든 itemListKeys 봐야 겠다
        foreach (int key in itemListKeys)
        {
            //해당 키값을 가지고 있지 않으면(해금하지 않았으면 넘긴다)
            if (!equipmentCount.ContainsKey(key))
                continue;
            if (equipmentCount[key] >= 3)
            {
                //키값의 인덱스를 찾고 1을 추가하여 다음 단계의 아이템 인덱스를 가지고 온다
                int nextIndex = itemListKeys[itemListKeys.IndexOf(key) + 1];
                
                //만약 다음 단계의 아이템이 락 상태면 해금 시킨다
                if (!equipmentCount.ContainsKey(nextIndex))
                {
                    equipmentCount[nextIndex] = 0;
                    //equipmentUnlock[nextIndex] = true;
                }
                //다음 단계의 아이템 인덱스의 갯수를 증가시킨다
                equipmentCount[nextIndex] += equipmentCount[key] / 3;
                //원래 단계의 아이템은 3개씩 나눈 나머지를 가진다
                equipmentCount[key] %= 3;

                //합성 이펙트 출력
                //합성 사운드 출력
            }
        }
        //ShowMyInventory();
        //일괄합성 버튼 비활성화 <- 다음에 아이템 얻을 경우마다 체크 후 출력
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
}
