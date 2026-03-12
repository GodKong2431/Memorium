using AYellowpaper.SerializedCollections;
using UnityEngine;

[CreateAssetMenu(fileName = "ToggleSpriteSO", menuName = "Scriptable Objects/ToggleSpriteSO")]
public class ToggleSpriteSO : ScriptableObject
{
    public SerializedDictionary<int, Sprite> ItemSpriteDict = new SerializedDictionary<int, Sprite>();
}
