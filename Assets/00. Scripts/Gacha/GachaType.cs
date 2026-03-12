/// <summary>
/// 가챠(뽑기) 종류.
/// 각 유형별로 별도 뽑기권·드롭 테이블 사용.
/// </summary>
public enum GachaType
{
    /// <summary>무기 뽑기. WeaponDrawTicket 사용.</summary>
    Weapon,

    /// <summary>방어구 뽑기. ArmorDrawTicket 사용. 투구/장화/장갑/갑옷 4종.</summary>
    Armor,

    /// <summary>스킬 주문서 뽑기. 기획서: 1회/10회만 지원.</summary>
    SkillScroll,

    /// <summary>Pixie 뽑기. (폐지) 사용하지 않음. 저장 호환용 enum 유지.</summary>
    Pixie
}
