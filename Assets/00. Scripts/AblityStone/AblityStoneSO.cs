using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "AblityStoneSO", menuName = "Scriptable Objects/AblityStoneSO")]
public class AblityStoneSO : ScriptableObject
{
    
    public SerializedDictionary<StoneGrade, AblityStone> AblityStone = new SerializedDictionary<StoneGrade, AblityStone>();

    public SerializedDictionary<PlayerStatType, StoneStatProbability> StoneStatProbability = new SerializedDictionary<PlayerStatType, StoneStatProbability>();
    
    
}
