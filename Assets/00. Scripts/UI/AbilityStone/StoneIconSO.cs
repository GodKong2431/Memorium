using AYellowpaper.SerializedCollections;
using UnityEngine;

public enum AblityStoneEnum
{
    None,
    Success,
    Fail,
    Decrease,
}

[System.Serializable]
public struct AblityStoneEnforce
{
    public Sprite enforceSprite;
    public Color myColor;
    
}

[CreateAssetMenu(fileName = "StoneIconSO", menuName = "Scriptable Objects/StoneIconSO")]
public class StoneIconSO : CurrencyIconSO
{
    [SerializeField] public SerializedDictionary<AblityStoneEnum, AblityStoneEnforce> EnforceSprites = new SerializedDictionary<AblityStoneEnum, AblityStoneEnforce>();
    
    [SerializeField] public Sprite LockSprite;
}
