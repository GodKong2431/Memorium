using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

public enum EquipmentID
{
    ID_100 = 100,
    ID_200 = 200,
    ID_300 = 300,
    ID_400 = 400,
    ID_500 = 500
}

public class TestExp : MonoBehaviour
{
    [SerializeField] Button button;

    [SerializeField] private CurrencyType currencyType;
    [SerializeField] private BigDouble amount;
    [SerializeField] private BigDouble currentAmount;

    [Space(10)]
    [SerializeField] Button buttone;

    [SerializeField] private PlayerStatType playerStatType;

    [Space(10)]
    [SerializeField] PlayerInventory playerInventory;
    [SerializeField] Button buttont;

    [SerializeField] EquipmentType equipmentType;
    [SerializeField] private EquipmentID ID;
    [Range(1,5)]
    [SerializeField] int eId;
    
    [SerializeField] int eAmount;

    

    [Space(10)]
    [SerializeField] Button killConutButton;
    [Range(1, 100)]
    [SerializeField] int count;

    private Dictionary<EquipmentType, int> equipmentKeyDict = new Dictionary<EquipmentType, int>()
{
    { EquipmentType.Boots, 3150000 },
    { EquipmentType.Armor, 3140000 },
    { EquipmentType.Gloves, 3130000 },
    { EquipmentType.Helmet, 3120000 },
    { EquipmentType.Weapon, 3110000 },
};

    private void Awake()
    {
        button.onClick.AddListener(() => CurrencyManager.Instance.AddCurrency(currencyType, amount));
        button.onClick.AddListener(() => currentAmount = CurrencyManager.Instance.GetAmount(currencyType));

        buttone.onClick.AddListener(() => CharacterStatManager.Instance.FinalStat(playerStatType));

        buttont.onClick.AddListener(() => playerInventory.IncreaseEquipment((equipmentKeyDict[equipmentType] + ((int)ID) + (eId)), eAmount));

        killConutButton.onClick.AddListener(() => EnemyKillRewardDispatcher.TotalKillCountUp(count));
    }
}
