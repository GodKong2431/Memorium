using UnityEngine;

public class PlayerEquipment : MonoBehaviour
{
    public EquipWeaponTable weapon;
    public EquipHelmetTable helmet;
    public EquipGloveTable glove;
    public EquipArmorTable armor;
    public EquipBootsTable boots;

    public void OnEqipItem(int itemId)
    {
        EquipListTable equipment = DataManager.Instance.EquipListDict[itemId];
        //플레이어 데이터에서 해당 능력치를 빼고 장착 후 다시 해당 능력치를 추가하는 코드
        //매니저에 해당 id 값을 저장하는 코드
        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                //플레이어 데이터에서 weapon 능력치를 빼는 코드
                weapon = DataManager.Instance.EquipWeaponDict[itemId];
                //player.attack += weapon.attackpower
                //장비창에 해당 테이블 이미지 넣는 코드
                break;
            case EquipmentType.Helmet:
                helmet = DataManager.Instance.EquipHelmetDict[itemId];
                break;
            case EquipmentType.Gloves:
                glove = DataManager.Instance.EquipGloveDict[itemId];
                break;
            case EquipmentType.Armor:
                armor = DataManager.Instance.EquipArmorDict[itemId];
                break;
            case EquipmentType.Boots:
                boots = DataManager.Instance.EquipBootsDict[itemId];
                break;
        }
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
            case EquipmentType.Gloves:
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

}
