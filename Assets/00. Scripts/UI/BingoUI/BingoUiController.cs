using System.Collections.Generic;
using UnityEngine;

public class BingoUiControlleer : MonoBehaviour
{
    [SerializeField] private List<RectTransform> Pages = new List<RectTransform>();
    
    public void OpenPage(RectTransform rectTransform)
    {
        if (!Pages.Contains(rectTransform))
        {
            return;
        }
        
        rectTransform.gameObject.SetActive(true);
    }
    
    public void ClosePage(RectTransform rectTransform)
    {
        if (!Pages.Contains(rectTransform))
        {
            return;
        }
        
        rectTransform.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        foreach(var page in Pages)
        {
            page.gameObject.SetActive(false);
        }
    }
}
