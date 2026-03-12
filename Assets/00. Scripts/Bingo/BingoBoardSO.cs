using UnityEngine;
using AYellowpaper.SerializedCollections;

[CreateAssetMenu(fileName = "BingoBoard", menuName = "Scriptable Objects/BingoBoard")]
public class BingoBoardSO : ScriptableObject
{
    public SerializedDictionary<int, SerializedDictionary<int, BingoSlot>> bingoSlots = new SerializedDictionary<int, SerializedDictionary<int, BingoSlot>>();
    
    public SerializedDictionary<SynergyDirection, SerializedDictionary<int, BingoSynergy>> bingoSynergy = new SerializedDictionary<SynergyDirection, SerializedDictionary<int, BingoSynergy>>();
}
