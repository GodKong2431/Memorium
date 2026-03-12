using UnityEngine;
using UnityEngine.UI;

public class ToggleBingoSlot : MonoBehaviour
{
    [SerializeField] private ItemBase _currentitem;
    public Image itemSprite;
    public bool isOccupied;
    public ItemBase currentitem
    {
        get {return _currentitem;}
        set
        {
            _currentitem = value;
            
            if (_currentitem != null)
            {
                itemSprite.sprite = _currentitem.itemSprite;
            }
            
            itemSprite.sprite = null;
        }
    }
    
    
    public void OnClick()
    {
        
    }
    
    public void SetImageSprite(Sprite sprite)
    {
        itemSprite.sprite = sprite;
    }
}
