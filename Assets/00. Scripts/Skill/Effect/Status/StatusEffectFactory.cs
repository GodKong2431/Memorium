
public static class StatusEffectFactory
{
    public static StatusEffectBase Create(int m5ID)
    {
        if (!DataManager.Instance.SkillModule5Dict.TryGetValue(m5ID, out var data))
            return null;

        return (M5Type)data.m5Type switch
        {
            M5Type.fire => new FireEffect(data),
            M5Type.poison => new PoisonEffect(data),
            M5Type.bleed => new BleedEffect(data),
            _ => null
        };
    }

    public static StatusEffectBase CreateFusion(int m5IDA, int m5IDB)
    {
        var fusion = FindFusion(m5IDA, m5IDB);
        if (fusion == null) return null;

        DataManager.Instance.SkillModule5Dict.TryGetValue(m5IDA, out var dataA);
        DataManager.Instance.SkillModule5Dict.TryGetValue(m5IDB, out var dataB);

        var pair = GetSortedPair((M5Type)dataA.m5Type, (M5Type)dataB.m5Type);

        var poisonData = (M5Type)dataA.m5Type == M5Type.poison ? dataA : dataB;
        var bleedData = (M5Type)dataA.m5Type == M5Type.bleed ? dataA : dataB;

        return pair switch
        {
            (M5Type.poison, M5Type.fire) => new IgnitionEffect(fusion),
            (M5Type.poison, M5Type.bleed) => new CorrosionEffect(fusion, poisonData),
            (M5Type.fire, M5Type.bleed) => new LacerationEffect(fusion, bleedData),
            _ => null
        };
    }

    private static M5FusionTable FindFusion(int idA, int idB)
    {
        foreach (var kv in DataManager.Instance.M5FusionDict)
        {
            var f = kv.Value;
            if ((f.m5IDA == idA && f.m5IDB == idB) ||
                (f.m5IDA == idB && f.m5IDB == idA))
                return f;
        }
        return null;
    }

    private static (M5Type, M5Type) GetSortedPair(M5Type a, M5Type b)
        => a <= b ? (a, b) : (b, a);
}