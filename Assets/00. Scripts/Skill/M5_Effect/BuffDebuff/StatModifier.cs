

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
}