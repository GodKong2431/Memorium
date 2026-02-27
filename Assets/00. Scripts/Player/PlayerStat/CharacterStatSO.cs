using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterStatSO", menuName = "Scriptable Objects/CharacterStatSO")]
public class CharacterStatSO : ScriptableObject
{
    public SerializedDictionary<PlayerStatType, StatUpgrade> Upgrades = new SerializedDictionary<PlayerStatType, StatUpgrade>();
    public SerializedDictionary<PlayerStatType, PlayerTrait> Traits = new SerializedDictionary<PlayerStatType, PlayerTrait>();
    
}
