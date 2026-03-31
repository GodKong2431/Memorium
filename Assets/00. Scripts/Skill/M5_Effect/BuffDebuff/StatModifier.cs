

using UnityEngine;

public struct StatModifier
{
    public int id;
    public StatType statType;
    public float value;
    public float duration;
    public float elapsedTime;

    public int GetID() => id;
    public float GetValue() => value;
    public bool IsExpired => duration > 0 && elapsedTime >= duration;

    public StatModifier(int id, StatType statType, float value, float duration, float elapsedTime = 0f)
    {
       this.id = id;
       this.statType = statType;
       this.value = value;
       this.duration = duration;
       this.elapsedTime = elapsedTime;
    }
}