using UnityEngine;

public class SizeChanger : MonoBehaviour
{
    [SerializeField] private RectTransform PanelArea;
    [SerializeField] private RectTransform contentsArea;
    
    public void SizeChange(float num)
    {
        Vector2 size = PanelArea.sizeDelta;
        size.y = num;
        PanelArea.sizeDelta = size;
    }
    
    public void SizeChangeContent(float num)
    {
        Vector2 size = contentsArea.sizeDelta;
        size.y = num;
        contentsArea.sizeDelta = size;
    }
    
}
