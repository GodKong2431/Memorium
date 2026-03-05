using System.Collections.Generic;

public class BuffDebuffHandler 
{
    private List<StatModifier> modifiers = new();

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
    }

    public void AddOrRefreshModifier(StatModifier modifier)
    {
        foreach (var existingModifier in modifiers)
        {
            if (existingModifier.GetID() == modifier.GetID())
            {
                existingModifier.Refesh();
                return;
            }
        }
        modifiers.Add(modifier);
    }

    public void Tick(float deltaTime)
    {
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            modifiers[i].Tick(deltaTime);
            if (modifiers[i].IsExpired)
            {
                modifiers.RemoveAt(i);
            }
        }
    }

    public float GetTotal(StatType playerStatType)
    {
        float total = 0f;
        foreach (var modifier in modifiers)
        {
            total += modifier.GetValue();
        }
        return total;
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
    }
}