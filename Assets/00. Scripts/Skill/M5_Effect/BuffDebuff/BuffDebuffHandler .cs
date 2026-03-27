using System.Collections.Generic;
using UnityEngine;

public class BuffDebuffHandler
{
    private List<StatModifier> modifiers = new();

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
    }

    public void AddOrRefreshModifier(StatModifier modifier)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            if (modifiers[i].GetID() == modifier.GetID())
            {
                var tmpMod = modifiers[i];
                tmpMod.elapsedTime = 0f;
                modifiers[i] = tmpMod;

                return;
            }
        }
        modifiers.Add(modifier);
    }

    public void Tick(float deltaTime)
    {
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            var tmpMod = modifiers[i];
            tmpMod.elapsedTime += deltaTime;
            if (tmpMod.IsExpired)
            {
                modifiers[i] = modifiers[modifiers.Count - 1];
                modifiers.RemoveAt(modifiers.Count - 1);
            }
            else
            {
                modifiers[i] = tmpMod;
            }
        }
    }

    public float GetTotal(StatType playerStatType)
    {
        float total = 0f;
        foreach (var modifier in modifiers)
        {
            if (modifier.statType != playerStatType)
                continue;

            total += modifier.GetValue();
        }
        return total;
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
    }
}
