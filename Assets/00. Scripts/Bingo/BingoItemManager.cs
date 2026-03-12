using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BingoItemManager : MonoBehaviour
{
    public ItemBase itemBase;
    
    public Toggle currentItem;
    
    public List<PluckItem> pluckItems = new List<PluckItem>();
    
    [SerializeField] private CanvasGroup bingobuttons;
    
    public void Test(bool use)
    {
        bingobuttons.blocksRaycasts = !use;
    }
    
}
