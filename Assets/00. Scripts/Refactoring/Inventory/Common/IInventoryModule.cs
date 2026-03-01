public interface IInventoryModule
{
    string ModuleName { get; } // 디버깅 시 어떤 모듈이 처리했는지 확인하기 위한 이름.

    // 전달된 ItemType을 이 모듈이 처리할 수 있는지 검사한다.
    bool CanHandle(ItemType itemType);

    // 공통 허브에서 전달한 아이템 추가 요청을 처리한다.
    bool TryAdd(InventoryItemContext item, BigDouble amount);

    // 공통 허브에서 전달한 아이템 차감 요청을 처리한다.
    bool TryRemove(InventoryItemContext item, BigDouble amount);

    // 공통 허브에서 전달한 아이템의 현재 보유량을 반환한다.
    BigDouble GetAmount(InventoryItemContext item);
}
