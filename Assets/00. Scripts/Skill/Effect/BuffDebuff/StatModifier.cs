

public class  StatModifier
{
    public int id;
    public StatType statType;
    public float value;
    public float duration;
    public float elapsedTime;

    public int GetID() => id;
    public float GetValue() => value;
    public void Tick(float deltaTime)
    {
        elapsedTime += deltaTime;
    }
    public void Refesh()
    {
        elapsedTime = 0f;
    }
    public bool IsExpired => duration > 0 && elapsedTime >= duration;
}