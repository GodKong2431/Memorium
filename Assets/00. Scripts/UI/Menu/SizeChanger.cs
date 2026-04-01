using UnityEngine;

public class SizeChanger : MonoBehaviour
{
    [SerializeField] private RectTransform PanelArea;
    [SerializeField] private RectTransform contentsArea;
    
    public void SizeChange(BottomPanelController bottomPanelController)
    {
        var num = bottomPanelController.GetMaxSheetHeight();
        
        Vector2 size = PanelArea.sizeDelta;
        size.y = num;
        PanelArea.sizeDelta = size;
        
        bottomPanelController.ApplySheetHeight(num);
    }
}
