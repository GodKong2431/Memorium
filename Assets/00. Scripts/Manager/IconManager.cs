#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public static class IconManager
{
    public static StatIconSO StatIconSO = Resources.Load<StatIconSO>("Icons/StatIconSO");
    public static StoneIconSO StoneIconSO = Resources.Load<StoneIconSO>("Icons/StoneIconSO");
}
