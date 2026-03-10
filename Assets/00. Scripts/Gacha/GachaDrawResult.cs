using System.Collections.Generic;

/// <summary>
/// 가챠 1회 또는 다회 뽑기 결과.
/// UI에서 연출·표시에 활용하면 될듯?
/// </summary>
public struct GachaDrawResult
{
    /// <summary>획득한 장비/스킬 주문서 아이템 ID 목록 (EquipListTable 또는 ItemInfoTable ID)</summary>
    public List<int> ItemIds;

    /// <summary>획득한 Pixie ID 목록 (FairyInfoTable ID)</summary>
    public List<int> PixieIds;

    /// <summary>뽑기 레벨이 상승했는지</summary>
    public bool LevelUp;

    /// <summary>소비한 재화 내역 (CurrencyType, 수량)</summary>
    public Dictionary<CurrencyType, int> SpentCurrencies;

    /// <summary>대박 아이템(4%, 1% 확률) 포함 여부. UI 연출용</summary>
    public bool HasRareItem;

    /// <summary>결과 초기화. TryDraw 호출 전에 반드시 사용.</summary>
    public static GachaDrawResult Create()
    {
        return new GachaDrawResult
        {
            ItemIds = new List<int>(),
            PixieIds = new List<int>(),
            SpentCurrencies = new Dictionary<CurrencyType, int>()
        };
    }
}
