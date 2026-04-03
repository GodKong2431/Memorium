/// <summary>
/// 프레젠테이션(이펙트·SFX 등) 구분. 값은 <see cref="MonsterType"/>과 동일 체계(1·2·3).
/// </summary>
public enum MonsterPresentationKind
{
    /// <summary>런타임에 몬스터 ID로 <see cref="MonsterDataProvider"/> 기준 타입 사용.</summary>
    Auto = 0,
    NormalMonster = 1,
    BossMonster = 2,
    SkillMonster = 3,
}
