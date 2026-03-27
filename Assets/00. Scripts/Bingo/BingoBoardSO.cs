using UnityEngine;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "BingoBoard", menuName = "Scriptable Objects/BingoBoard")]
public class BingoBoardSO : ScriptableObject
{
    public BingoSynergy bingoSynergy;
    public SerializedDictionary<RarityType, BingoSlot> RaritySolts = new SerializedDictionary<RarityType, BingoSlot>(); 
    
    public SerializedDictionary<int, SerializedDictionary<int, RarityType>> bingoSlots = new SerializedDictionary<int, SerializedDictionary<int, RarityType>>();
    
    public SerializedDictionary<SynergyDirection, SerializedDictionary<int, BingoSynergy>> bingoSynergys = new SerializedDictionary<SynergyDirection, SerializedDictionary<int, BingoSynergy>>();
}
