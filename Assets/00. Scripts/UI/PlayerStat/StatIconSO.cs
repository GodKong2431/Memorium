using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "StatIconSO", menuName = "Scriptable Objects/StatIconSO")]
public class StatIconSO : CurrencyIconSO
{
    [SerializeField] public SerializedDictionary<StatType, Sprite> StatIconDict = new SerializedDictionary<StatType, Sprite>(); 
}
