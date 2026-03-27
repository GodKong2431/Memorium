using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "SynergyIconSO", menuName = "Scriptable Objects/SynergyIconSO")]
public class SynergyIconSO : ScriptableObject
{
    public SerializedDictionary<SynergyStat, Sprite> SynergyIcons = new SerializedDictionary<SynergyStat, Sprite>();
}
