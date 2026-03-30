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
    [SerializeField] public SynergyDirection bingoSynergyLine;//enumCount
    [SerializeField] private SynergyData synergyData;
    [SerializeField] private Image statIcon;
    [SerializeField] private Image frameImage;
    [Header("Frame Colors")]
    [SerializeField] private Color normalFrameColor = new Color32(255, 255, 255, 255);
    [SerializeField] private Color uncommonFrameColor = new Color32(46, 196, 148, 255);
    [SerializeField] private Color rareFrameColor = new Color32(142, 74, 255, 255);
    [SerializeField] private Color legendaryFrameColor = new Color32(214, 162, 45, 255);
    [SerializeField] private Color mythicFrameColor = new Color32(255, 133, 33, 255);
    
    public bool isBingo;
    
    public SynergyData SynergyData
    {
        get {return synergyData;}
        set
        {
            synergyData = value;

            if (synergyData == null)
            {
                if (statIcon != null)
                {
                    statIcon.sprite = null;
                    statIcon.color = Color.white;
                }

                ApplyFrameColor(normalFrameColor);
                return;
            }

            StatType1 = synergyData.statType1;
            StatType2 = synergyData.statType2;
            IncreaseStat1 = synergyData.statUp1;
            IncreaseStat2 = synergyData.StatUp2;

            if (statIcon != null)
            {
                Sprite resolvedIcon = IconManager.GetSynergyIcon(synergyData.synergyStat);
                if (resolvedIcon == null && synergyData.ID > 0)
                {
                    int statIdFromId = synergyData.ID % 1000;
                    if (Enum.IsDefined(typeof(SynergyStat), statIdFromId))
                        resolvedIcon = IconManager.GetSynergyIcon((SynergyStat)statIdFromId);
                }

                statIcon.sprite = resolvedIcon;
                statIcon.color = Color.white;
            }

            ApplyFrameColor(GetFrameColorByRarity(synergyData.rarityType));
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
    
    public void SetSynergy(int key)
    {
        SynergyManager manager = SynergyManager.Instance;
        int resolvedKey = key > 0 ? key : 3431001;

        if (manager == null)
        {
            Debug.LogError($"[BingoSynergy] SynergyManager instance was not found while resolving synergy id {key}.", this);
            SynergyData = CreateLocalFallbackSynergyData(resolvedKey);
            return;
        }

        if (manager.TryGetSynergyData(resolvedKey, out var resolvedSynergyData))
        {
            SynergyData = resolvedSynergyData;
            return;
        }

        if (manager.TryGetAnySynergyData(out var fallbackSynergyData))
        {
            Debug.LogWarning($"[BingoSynergy] Failed to resolve synergy id {key}. Fallback synergy was applied.", this);
            SynergyData = fallbackSynergyData;
            return;
        }

        Debug.LogError($"[BingoSynergy] Failed to resolve synergy id {key} and no fallback synergy was found.", this);
        SynergyData = CreateLocalFallbackSynergyData(resolvedKey);
    }
    

    private void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = "0";
    }
    public void Check()
    {
        if (BingoBoardManager.Instance.CheckLine(bingoSynergyLine, index))
        {
            isBingo = true;
            CharacterStatManager.Instance.FinalStat(statType1);
            CharacterStatManager.Instance.FinalStat(statType2);
        }
        
    }
    
    public Sprite GetIcon()
    {
        return statIcon.sprite;
    }

    private void ApplyFrameColor(Color targetColor)
    {
        if (frameImage == null)
            return;

        frameImage.color = targetColor;
    }

    private Color GetFrameColorByRarity(RarityType rarityType)
    {
        switch (rarityType)
        {
            case RarityType.normal:
                return normalFrameColor;
            case RarityType.uncommon:
                return uncommonFrameColor;
            case RarityType.rare:
                return rareFrameColor;
            case RarityType.legendary:
                return legendaryFrameColor;
            case RarityType.mythic:
                return mythicFrameColor;
            default:
                return normalFrameColor;
        }
    }

    private SynergyData CreateLocalFallbackSynergyData(int synergyId)
    {
        int safeId = synergyId > 0 ? synergyId : 3431001;
        int statId = safeId % 1000;
        if (!Enum.IsDefined(typeof(SynergyStat), statId) || statId == (int)SynergyStat.None)
            statId = (int)SynergyStat.ATK;

        int rarityIndex = Mathf.Clamp(((safeId / 1000) % 10) - 1, 0, (int)RarityType.mythic);
        RarityType rarity = (RarityType)rarityIndex;

        SynergyData fallback = new SynergyData();
        fallback.Init((SynergyStat)statId);
        fallback.ID = safeId;
        fallback.rarityType = rarity;
        fallback.statUp1 = 0f;
        fallback.StatUp2 = 0f;
        return fallback;
    }
}
