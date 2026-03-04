using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatSO", menuName = "Scriptable Objects/CharacterStatSO")]
public class CharacterStatSO : ScriptableObject
{
    public SerializedDictionary<StatType, StatUpgrade> Upgrades = new SerializedDictionary<StatType, StatUpgrade>();
    public SerializedDictionary<StatType, PlayerTrait> Traits = new SerializedDictionary<StatType, PlayerTrait>();
    
}
