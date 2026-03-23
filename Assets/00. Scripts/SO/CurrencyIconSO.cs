using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CurrencyIconSO", menuName = "Scriptable Objects/CurrencyIconSO")]

public class CurrencyIconSO : ScriptableObject
{
    public SerializedDictionary<CurrencyType, Sprite> CurrencyIconDict = new SerializedDictionary<CurrencyType, Sprite>();
}
