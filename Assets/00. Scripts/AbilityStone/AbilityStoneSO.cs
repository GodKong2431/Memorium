using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityStoneSO", menuName = "Scriptable Objects/AbilityStoneSO")]
public class AblityStoneSO : ScriptableObject
{
    
    public SerializedDictionary<StoneGrade, AbilityStone> AbilityStoneDict = new SerializedDictionary<StoneGrade, AbilityStone>();

    public SerializedDictionary<StatType, StoneStatProbability> StoneStatProbabilityDict = new SerializedDictionary<StatType, StoneStatProbability>();

    public SerializedDictionary<int, StoneTotalUpBonus> StoneTotalUpBonusDict = new SerializedDictionary<int, StoneTotalUpBonus>();
}
