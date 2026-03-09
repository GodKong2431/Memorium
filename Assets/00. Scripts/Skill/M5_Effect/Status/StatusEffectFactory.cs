
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

    //테스트용 하드코딩
    //테스트용 하드코딩
    public static StatusEffectBase CreateFusion(SkillModule5Table dataA, SkillModule5Table dataB)
    {
        var typeA = dataA.m5Type;
        var typeB = dataB.m5Type;

        // [로그 1단계] 어떤 속성 두 개가 섞이려고 들어왔는지 확인!
        Debug.Log($"[퓨전 1단계] 진입 성공 -> typeA: {typeA}, typeB: {typeB}");

        int fusionID = (typeA, typeB) switch
        {
            (M5Type.poison, M5Type.fire) or (M5Type.fire, M5Type.poison) => 4060001,
            (M5Type.poison, M5Type.bleed) or (M5Type.bleed, M5Type.poison) => 4060002,
            (M5Type.fire, M5Type.bleed) or (M5Type.bleed, M5Type.fire) => 4060003,
            _ => 0
        };

        // [로그 2단계] 조합식 자체가 없는 경우 (예: 출혈+출혈 등)
        if (fusionID == 0)
        {
            Debug.LogWarning($"[퓨전 실패] 매칭되는 퓨전 조합이 없습니다! (입력된 타입: {typeA} + {typeB})");
            return null;
        }

        Debug.Log($"[퓨전 2단계] 조합 매칭 성공 -> 할당된 퓨전 ID: {fusionID}");

        // [로그 3단계] 딕셔너리(CSV)에 해당 ID 데이터가 없는 경우
        if (!DataManager.Instance.M5FusionDict.TryGetValue(fusionID, out var fusionData))
        {
            Debug.LogError($"[퓨전 치명적 실패] M5FusionDict 안에 ID {fusionID} 데이터가 없습니다! (데이터가 로드되지 않았거나 인젝터 주입이 누락됨)");
            return null;
        }

        Debug.Log($"[퓨전 3단계] 딕셔너리에서 데이터 찾기 성공 -> 퓨전 이름: {fusionData.fusionName}");

        StatusEffectBase effect = fusionID switch
        {
            4060001 => new IgnitionEffect(fusionData),
            4060002 => new CorrosionEffect(fusionData, typeA == M5Type.poison ? dataA : dataB),
            4060003 => new LacerationEffect(fusionData, typeA == M5Type.bleed ? dataA : dataB),
            _ => null
        };

        // [로그 4단계] 최종 객체 생성 확인
        if (effect == null)
        {
            Debug.LogError("[퓨전 실패] 객체 생성 switch문에서 알 수 없는 이유로 null이 반환되었습니다.");
        }
        else
        {
            Debug.Log($"[퓨전 최종 성공!] 생성된 이펙트 객체: {effect.GetType().Name}");
        }

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