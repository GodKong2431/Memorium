
public static class StatusEffectFactory
{
    public static StatusEffectBase Create(SkillModule5Table data)
    {

        return (data.m5Type)switch
        {
            M5Type.fire => new FireEffect(data),
            M5Type.poison => new PoisonEffect(data),
            M5Type.bleed => new BleedEffect(data),
            _ => null
        };
    }
    public static StatusEffectBase CreateFusion(SkillModule5Table dataA, SkillModule5Table dataB)
    {
        var fusion = FindFusion(dataA.ID, dataB.ID);
        if (fusion == null) return null;

        var pair = GetSortedPair(dataA.m5Type, dataB.m5Type);

        var poisonData = dataA.m5Type == M5Type.poison ? dataA : dataB;
        var bleedData = dataA.m5Type == M5Type.bleed ? dataA : dataB;

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
        foreach (var keyV in DataManager.Instance.M5FusionDict)
        {
            var find = keyV.Value;
            if ((find.m5IDA == idA && find.m5IDB == idB) ||
                (find.m5IDA == idB && find.m5IDB == idA))
                return find;
        }
        return null;
    }

    private static (M5Type, M5Type) GetSortedPair(M5Type a, M5Type b)
        => a <= b ? (a, b) : (b, a);
}