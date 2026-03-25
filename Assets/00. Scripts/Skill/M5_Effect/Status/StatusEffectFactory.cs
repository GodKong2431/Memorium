
using UnityEngine;

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
        var typeA = dataA.m5Type;
        var typeB = dataB.m5Type;


        int fusionID = (typeA, typeB) switch
        {
            (M5Type.poison, M5Type.fire) or (M5Type.fire, M5Type.poison) => 4060001,
            (M5Type.poison, M5Type.bleed) or (M5Type.bleed, M5Type.poison) => 4060002,
            (M5Type.fire, M5Type.bleed) or (M5Type.bleed, M5Type.fire) => 4060003,
            _ => 0
        };


        if (!DataManager.Instance.M5FusionDict.TryGetValue(fusionID, out var fusionData))
        {
           
            return null;
        }


        StatusEffectBase effect = fusionID switch
        {
            4060001 => new IgnitionEffect(fusionData),
            4060002 => new CorrosionEffect(fusionData, typeA == M5Type.poison ? dataA : dataB),
            4060003 => new LacerationEffect(fusionData, typeA == M5Type.bleed ? dataA : dataB),
            _ => null
        };


        return effect;
    }
    //    public static StatusEffectBase CreateFusion(SkillModule5Table dataA, SkillModule5Table dataB)
    //{
    //    var fusion = FindFusion(dataA.ID, dataB.ID);
    //    if (fusion == null)  return null;

    //    var typeA = dataA.m5Type;
    //    var typeB = dataB.m5Type;

    //    var poisonData = typeA == M5Type.poison ? dataA : (typeB == M5Type.poison ? dataB : null);
    //    var bleedData = typeA == M5Type.bleed ? dataA : (typeB == M5Type.bleed ? dataB : null);

    //    return (typeA, typeB) switch
    //    {
    //        (M5Type.poison, M5Type.fire) or (M5Type.fire, M5Type.poison) => new IgnitionEffect(fusion),
    //        (M5Type.poison, M5Type.bleed) or (M5Type.bleed, M5Type.poison) => new CorrosionEffect(fusion, poisonData),
    //        (M5Type.fire, M5Type.bleed) or (M5Type.bleed, M5Type.fire) => new LacerationEffect(fusion, bleedData),
    //        _ => null
    //    };
    //}

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
}