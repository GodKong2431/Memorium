using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class InventoryManager : Singleton<InventoryManager>
{
    private readonly List<IInventoryModule> modules = new List<IInventoryModule>(); // 허브에 등록된 인벤토리 모듈 목록.
    private readonly Dictionary<ItemType, IInventoryModule> routeByItemType = new Dictionary<ItemType, IInventoryModule>(); // ItemType별 라우팅 캐시.

    public event Action<InventoryItemContext, BigDouble> OnItemAmountChanged; // 공통 아이템 수량 변경 이벤트.

    // 런타임 시작 시 기본 모듈을 한 번만 등록한다.

    //재화를 저장할 객체
    public SaveCurrencyData saveCurrencyData;
    public bool DataLoad = false;

    protected override void Awake()
    {
        base.Awake();
        InitializeModulesOnce();
    }

    // 외부 모듈 등록 진입점이다.
    public void RegisterModule(IInventoryModule module)
    {
        if (module == null)
            return;
        if (modules.Contains(module))
            return;

        modules.Add(module);
        routeByItemType.Clear();
    }

    // 외부 모듈 해제 진입점이다.
    public void UnregisterModule(IInventoryModule module)
    {
        if (module == null)
            return;
        if (!modules.Remove(module))
            return;

        routeByItemType.Clear();
    }

    // 모듈 타입으로 등록된 모듈을 조회한다.
    public TModule GetModule<TModule>() where TModule : class, IInventoryModule
    {
        for (int i = 0; i < modules.Count; i++)
        {
            if (modules[i] is TModule typed)
                return typed;
        }

        return null;
    }

    // itemId를 해석해 적절한 모듈에 아이템 추가를 요청한다.
    public bool AddItem(int itemId, BigDouble amount)
    {
        return TryProcessItem(itemId, amount, isAdd: true);
    }

    // 편의용 정수 수량 오버로드다.
    public bool AddItem(int itemId, int amount)
    {
        return AddItem(itemId, new BigDouble(amount));
    }


    public bool SetItem(int itemId, BigDouble amount)
    {
        BigDouble curCount=GetItemAmount(itemId);
        if ((curCount>0))
        {
            if (!RemoveItem(itemId, curCount))
            {
                return false;
            }
        }

        if (!AddItem(itemId, amount))
        {
            return false;
        }

        return true;
    }
    public bool SetItem(int itemId, int amount)
    {
        BigDouble curCount=GetItemAmount(itemId);
        if ((curCount>0))
        {
            if (!RemoveItem(itemId, curCount))
            {
                return false;
            }
        }

        if (!AddItem(itemId, amount))
        {
            return false;
        }

        return true;
    }

    // itemId를 해석해 적절한 모듈에 아이템 차감을 요청한다.
    public bool RemoveItem(int itemId, BigDouble amount)
    {
        return TryProcessItem(itemId, amount, isAdd: false);
    }

    // 편의용 정수 수량 오버로드다.
    public bool RemoveItem(int itemId, int amount)
    {
        return RemoveItem(itemId, new BigDouble(amount));
    }

    // itemId의 현재 보유 수량을 반환한다.
    public BigDouble GetItemAmount(int itemId)
    {
        if (!TryCreateContext(itemId, out var item))
            return BigDouble.Zero;
        if (!TryGetModule(item.ItemType, out var module))
            return BigDouble.Zero;

        return module.GetAmount(item);
    }

    // itemId의 보유 수량이 요구량 이상인지 확인한다.
    public bool HasEnoughItem(int itemId, BigDouble requiredAmount)
    {
        if (requiredAmount <= BigDouble.Zero)
            return true;

        return GetItemAmount(itemId) >= requiredAmount;
    }

    // Add/Remove 공통 흐름을 처리한다.
    private bool TryProcessItem(int itemId, BigDouble amount, bool isAdd)
    {
        if (amount <= BigDouble.Zero)
            return false;
        if (!TryCreateContext(itemId, out var item))
            return false;
        if (!TryGetModule(item.ItemType, out var module))
            return false;

        bool processed = isAdd ? module.TryAdd(item, amount) : module.TryRemove(item, amount);
        if (!processed)
            return false;

        PublishItemChanged(item, module);
        return true;
    }

    // itemId를 itemType과 묶어서 컨텍스트로 만든다.
    private bool TryCreateContext(int itemId, out InventoryItemContext item)
    {
        item = default;

        if (!TryResolveItemType(itemId, out var itemType))
            return false;

        item = new InventoryItemContext(itemId, itemType);
        return true;
    }

    // ItemInfoDict/EquipListDict를 순서대로 확인해 itemType을 해석한다.
    private static bool TryResolveItemType(int itemId, out ItemType itemType)
    {
        itemType = default;

        if (DataManager.Instance == null)
            return false;

        if (DataManager.Instance.ItemInfoDict != null &&
            DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out var itemInfo))
        {
            itemType = itemInfo.itemType;
            return true;
        }

        if (DataManager.Instance.EquipListDict != null &&
            DataManager.Instance.EquipListDict.TryGetValue(itemId, out var equipInfo))
        {
            itemType = (ItemType)equipInfo.equipmentType;
            return true;
        }

        return false;
    }

    // ItemType을 처리할 수 있는 모듈을 라우팅 캐시에서 찾는다.
    private bool TryGetModule(ItemType itemType, out IInventoryModule module)
    {
        if (routeByItemType.TryGetValue(itemType, out module))
            return true;

        for (int i = 0; i < modules.Count; i++)
        {
            var candidate = modules[i];
            if (candidate == null)
                continue;
            if (!candidate.CanHandle(itemType))
                continue;

            module = candidate;
            routeByItemType[itemType] = candidate;
            return true;
        }

        module = null;
        Debug.LogWarning($"[InventoryManager] 처리 가능한 모듈이 없습니다. ItemType: {itemType}");
        return false;
    }

    // 모듈 처리 후 최신 수량을 읽어 공통 변경 이벤트를 발행한다.
    private void PublishItemChanged(InventoryItemContext item, IInventoryModule module)
    {
        BigDouble currentAmount = module.GetAmount(item);
        OnItemAmountChanged?.Invoke(item, currentAmount);
    }

    // 기본 모듈을 1회만 생성/등록한다.
    private void InitializeModulesOnce()
    {
        if (modules.Count > 0)
            return;

        RegisterModule(new CurrencyInventoryModule());
        RegisterModule(new SkillInventoryModule());
        RegisterModule(new StackItemInventoryModule());
        RegisterModule(new EquipmentInventoryModule());
        RegisterModule(new PixieInventoryModule());

        StartCoroutine(LoadData());
    }

    IEnumerator LoadData()
    {
        yield return new WaitUntil(() => DataManager.Instance != null);
        yield return new WaitUntil(() => DataManager.Instance.DataLoad);
        saveCurrencyData = JSONService.Load<SaveCurrencyData>();
        saveCurrencyData.InitCurrencyData();
        saveCurrencyData.SetData();
        OnItemAmountChanged += saveCurrencyData.Save;
        DataLoad = true;
    }
    //private void OnDisable ()
    //{
    //    if (!DataLoad)
    //        return;

    //    JSONService.Save(saveCurrencyData);
    //}

    //public async Task AutoSaveTask()
    //{
    //    Debug.Log("[InventoryManager] 재화 변경사항 확인 및 데이터 저장");
    //    await JSONService.SaveFileOnAsync(saveCurrencyData);
    //    saveCurrencyData.ClearDirty();
    //}

    //protected override void OnApplicationQuit()
    //{
    //    if (!DataLoad)
    //        return;
    //    //foreach (CurrencyType currencyType in Enum.GetValues(typeof(CurrencyType)))
    //    //{
    //    //    if (!saveCurrencyData.currencyTypeToKey.ContainsKey(currencyType))
    //    //    {
    //    //        Debug.Log($"[InventoryManager] 데이터 저장 시도 : {currencyType} 반환");
    //    //        continue;
    //    //    }
    //    //    Debug.Log($"[InventoryManager] 데이터 저장 시도 : {currencyType}");
    //    //    saveCurrencyData.SaveBeforeQuit(currencyType, GetItemAmount(saveCurrencyData.currencyTypeToKey[currencyType]));
    //    //}
    //    //saveCurrencyData.SaveBeforeQuit();
    //    JSONService.Save(saveCurrencyData);

    //}
}
