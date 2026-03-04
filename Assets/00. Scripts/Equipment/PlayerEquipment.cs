using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerEquipment : MonoBehaviour
{
    public EquipWeaponTable weapon;
    public EquipHelmetTable helmet;
    public EquipGloveTable glove;
    public EquipArmorTable armor;
    public EquipBootsTable boots;

    //0 : 무기 1: 모자 2: 장갑 3:갑옷 4:신발
    public GameObject[] onEquipmentUI;

    public EquipmentHandler equipmentHandler;

    public void OnEqipItem(int itemId)
    {
        //Debug.Log($"[PlayerEquipment] ID : {itemId} 착용 시도");
        if (!DataManager.Instance.EquipListDict.ContainsKey(itemId))
        {
            //Debug.Log($"[PlayerEquipment] 아이템 번호 : {itemId}가 존재하지 않습니다.");
            return;
        }

        EquipListTable equipment = DataManager.Instance.EquipListDict[itemId];
        //플레이어 데이터에서 해당 능력치를 빼고 장착 후 다시 해당 능력치를 추가하는 코드
        //매니저에 해당 id 값을 저장하는 코드
        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                //플레이어 데이터에서 weapon 능력치를 빼는 코드 <- 해당 코드 작성 시 처음에 null이면 예외처리로 그냥 넘기고 바로 장착 할 것
                //if (weapon != null)
                //{
                //    TestPlayerDataManager.Instance.playerAttack -= weapon.attackPower;
                //    TestPlayerDataManager.Instance.playerAttackSpeed -= weapon.attackSpeed;
                //}
                weapon = DataManager.Instance.EquipWeaponDict[itemId];
                //TestPlayerDataManager.Instance.playerAttack += weapon.attackPower;
                //TestPlayerDataManager.Instance.playerAttackSpeed += weapon.attackSpeed;
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Weapon);

                //장비창에 해당 테이블 이미지 넣는 코드
                break;
            case EquipmentType.Helmet:
                //if (helmet != null)
                //    TestPlayerDataManager.Instance.playerDefense -= helmet.defense;
                helmet = DataManager.Instance.EquipHelmetDict[itemId];
                //TestPlayerDataManager.Instance.playerDefense += helmet.defense;

                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Helmet);

                break;
            case EquipmentType.Glove:
                glove = DataManager.Instance.EquipGloveDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Glove);

                break;
            case EquipmentType.Armor:
                armor = DataManager.Instance.EquipArmorDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Armor);

                break;
            case EquipmentType.Boots:
                boots = DataManager.Instance.EquipBootsDict[itemId];
                CharacterStatManager.Instance.PlayerSlot.SetSlot(itemId, SlotType.Boots);
                break;
        }

        ChangeOnEquipmentUIImage(equipment.equipmentType, itemId);
        //Debug.Log($"[PlayerEquipment] 아이템 장착 : ${equipment.equipmentName}");
    }

    public int ReturnItemNum(EquipmentType equipmentType)
    {
        int itemId = 0;
        switch (equipmentType)
        {
            case EquipmentType.Weapon:
                itemId = weapon.ID;
                break;
            case EquipmentType.Helmet:
                itemId = helmet.ID;
                break;
            case EquipmentType.Glove:
                itemId = glove.ID;
                break;
            case EquipmentType.Armor:
                itemId = armor.ID;
                break;
            case EquipmentType.Boots:
                itemId = boots.ID;
                break;
        }

        return itemId;
    }

    public void ChangeOnEquipmentUIImage(EquipmentType type, int itemId)
    {
        if (onEquipmentUI == null)
            return;

        int indexNum = (int)type - (int)EquipmentType.Weapon;
        if (indexNum < 0 || indexNum >= onEquipmentUI.Length)
            return;

        GameObject targetSlot = onEquipmentUI[indexNum];
        if (targetSlot == null)
            return;

        if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable equipInfo))
            return;

        Image slotImage = targetSlot.GetComponent<Image>();
        if (slotImage != null)
        {
            // Sub menu toggle 슬롯은 선택/비선택 색을 별도 UI 컨트롤러가 관리한다.
            // 여기서 rarity 색을 덮어쓰면 탭 색상과 충돌하므로 흰색을 유지한다.
            if (targetSlot.GetComponent<Toggle>() != null)
                slotImage.color = Color.white;
            else
                slotImage.color = RarityColor.ItemGradeColor(equipInfo.rarityType);
        }

        string label = equipInfo.description + "\n" + equipInfo.equipmentName;

        Text legacyText = targetSlot.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = label;

        TMP_Text tmpText = targetSlot.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = label;
    }
}
