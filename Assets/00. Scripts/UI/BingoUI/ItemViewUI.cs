using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemViewUI : MonoBehaviour
{
    [SerializeField] private Image itemIcon;
    [SerializeField] private TextMeshProUGUI itemCount;
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private TextMeshProUGUI itemDesc;


    void Start()
    {
        BingoItemManager.OnChangedItem += UpdateUI;
        BingoItemManager.OnChangedItem += OpenPopUp;
        gameObject.SetActive(false);
    }
    
    void OnDisable()
    {
        gameObject.SetActive(false);
    }
    void OpenPopUp(ItemBase currentItem)
    {
        if(currentItem == null)
            return;
        gameObject.SetActive(true);
    
    }

    void UpdateUI(ItemBase currentItem)
    {
        if(currentItem == null)
        {
            NullItem();
            return;
        }
        
        itemIcon.sprite = currentItem.itemSprite.sprite;
        itemCount.text = currentItem.itemCount.ToString();
        itemName.text = DataManager.Instance.ItemInfoDict[currentItem.itemInfoID].itemName;
        itemDesc.text = DataManager.Instance.ItemInfoDict[currentItem.itemInfoID].itemDesc;
    }
    
    void NullItem()
    {
        gameObject.SetActive(false);
    }
}
