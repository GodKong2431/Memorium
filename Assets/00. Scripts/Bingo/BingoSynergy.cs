using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


[System.Serializable]
public class BingoSynergy : MonoBehaviour
{
    [SerializeField] private StatType statType1;
    [SerializeField] private StatType statType2;
    [SerializeField] private float increaseStat1;
    [SerializeField] private float increaseStat2;
    [SerializeField] public int index;
    [SerializeField] public SynergyDirection bingoSynergyLine;
    [SerializeField] private SynergyData synergyData;
    [SerializeField] private Image statIcon;
    
    public bool isBingo;
    
    public SynergyData SynergyData
    {
        get {return synergyData;}
        set
        {
            synergyData = value;
            StatType1 = value.statType1;
            StatType2 = value.statType2;
            IncreaseStat1 = value.statUp1;
            IncreaseStat2 = value.StatUp2;
        }
    }
    private TextMeshProUGUI text;
    
    public StatType StatType1 {get {return statType1;} private set { statType1 = value; }}
    public StatType StatType2 {get {return statType2;} private set { statType2 = value; }}
    public float IncreaseStat1
    {
        get
        {
            return increaseStat1;
        }
        set
        {
            increaseStat1 = value * (bingoSynergyLine == SynergyDirection.Diagonal ? 2 : 1);
        }
    }
    
    public float IncreaseStat2
    {
        get
        {
            return increaseStat2;
        }
        set
        {
            increaseStat2 = value * (bingoSynergyLine == SynergyDirection.Diagonal ? 2 : 1);
        }
    }
    
    public void Init(SynergyDirection bingoSynergyLine , int index)
    {
        this.bingoSynergyLine = bingoSynergyLine;
        this.index = index;
    }

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = "0";
    }
    public void Check()
    {
        if (BingoBoard.Instance.CheckLine(bingoSynergyLine, index))
        {
            isBingo = true;
            CharacterStatManager.Instance.FinalStat(statType1);
            CharacterStatManager.Instance.FinalStat(statType2);
        }
        
    }
}
