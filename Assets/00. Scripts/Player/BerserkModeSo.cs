using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "BerserkModeSo", menuName = "Scriptable Objects/BerserkModeSo")]
public class BerserkModeSo : ScriptableObject
{
    [SerializeField] public SerializedDictionary<StatType, float> BserserkMultStatSo = new SerializedDictionary<StatType, float>();
}
