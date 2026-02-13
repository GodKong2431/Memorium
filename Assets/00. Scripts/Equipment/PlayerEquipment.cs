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
        Debug.Log($"[PlayerEquipment] ID : {itemId} 착용 시도");
        EquipListTable equipment = DataManager.Instance.EquipListDict[itemId];
        //플레이어 데이터에서 해당 능력치를 빼고 장착 후 다시 해당 능력치를 추가하는 코드
        //매니저에 해당 id 값을 저장하는 코드
        switch (equipment.equipmentType)
        {
            case EquipmentType.Weapon:
                //플레이어 데이터에서 weapon 능력치를 빼는 코드 <- 해당 코드 작성 시 처음에 null이면 예외처리로 그냥 넘기고 바로 장착 할 것
                if (weapon != null)
                {
                    TestPlayerDataManager.Instance.playerAttack -= weapon.attackPower;
                    TestPlayerDataManager.Instance.playerAttackSpeed -= weapon.attackSpeed;
                }
                weapon = DataManager.Instance.EquipWeaponDict[itemId];
                TestPlayerDataManager.Instance.playerAttack += weapon.attackPower;
                TestPlayerDataManager.Instance.playerAttackSpeed += weapon.attackSpeed;

                //장비창에 해당 테이블 이미지 넣는 코드
                break;
            case EquipmentType.Helmet:
                if (helmet != null)
                    TestPlayerDataManager.Instance.playerDefense -= helmet.defense;
                helmet = DataManager.Instance.EquipHelmetDict[itemId];
                TestPlayerDataManager.Instance.playerDefense += helmet.defense;
                break;
            case EquipmentType.Gloves:
                if(glove != null)
                    TestPlayerDataManager.Instance.playerMagicDefense -= glove.magicDefense;
                glove = DataManager.Instance.EquipGloveDict[itemId];
                TestPlayerDataManager.Instance.playerMagicDefense += glove.magicDefense;
                break;
            case EquipmentType.Armor:
                if (armor != null)
                    TestPlayerDataManager.Instance.playerHp -= armor.hp;
                armor = DataManager.Instance.EquipArmorDict[itemId];
                TestPlayerDataManager.Instance.playerHp += armor.hp;
                break;
            case EquipmentType.Boots:
                if (boots != null)
                    TestPlayerDataManager.Instance.playerMoveSpeed -= boots.moveSpeed;
                boots = DataManager.Instance.EquipBootsDict[itemId];
                TestPlayerDataManager.Instance.playerMoveSpeed += boots.moveSpeed;
                break;
        }
        Debug.Log($"[PlayerEquipment] 아이템 장착 : ${equipment.equipmentName}");
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
