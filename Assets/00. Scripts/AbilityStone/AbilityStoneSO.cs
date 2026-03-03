using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityStoneSO", menuName = "Scriptable Objects/AbilityStoneSO")]
public class AblityStoneSO : ScriptableObject
{
    
    public SerializedDictionary<StoneGrade, AbilityStone> AbilityStone = new SerializedDictionary<StoneGrade, AbilityStone>();

    public SerializedDictionary<PlayerStatType, StoneStatProbability> StoneStatProbability = new SerializedDictionary<PlayerStatType, StoneStatProbability>();
    
    
}
