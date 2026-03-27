using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SynergyViewItem : MonoBehaviour
{
    [SerializeField] public Image icon;
    [SerializeField] public TextMeshProUGUI text;
    
    public void SetView(BingoSynergy bingoSynergy)
    {
        icon.sprite = bingoSynergy.GetIcon();
        
        text.text = $"+ {bingoSynergy.IncreaseStat1*100}%";
    }
}
