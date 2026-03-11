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

#if UNITY_EDITOR
                Debug.Log($"스텟 버프: {tmpMod.statType} : {tmpMod.GetValue()}갱신");
#endif
                return;
            }
        }
        modifiers.Add(modifier);
#if UNITY_EDITOR
        Debug.Log($"스텟 버프: {modifier.statType} : {modifier.GetValue()}적용");
#endif
    }

    public void Tick(float deltaTime)
    {
        for (int i = modifiers.Count - 1; i >= 0; i--)
        {
            var tmpMod = modifiers[i];
            tmpMod.elapsedTime += deltaTime;
            if (tmpMod.IsExpired)
            {
#if UNITY_EDITOR
                Debug.Log($"스텟 버프: {tmpMod.statType} : {tmpMod.GetValue()} 종료");
#endif
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
            total += modifier.GetValue();
        }
        return total;
    }

    public void ClearModifiers()
    {
        modifiers.Clear();
    }
}