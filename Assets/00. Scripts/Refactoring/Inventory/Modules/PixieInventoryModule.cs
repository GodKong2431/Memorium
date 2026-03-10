
using System;
using System.Collections.Generic;


public sealed class PixieInventoryModule : IInventoryModule
{
    private readonly Dictionary<int, OwnedPixieData> pixieDict = new Dictionary<int, OwnedPixieData>();
    public IEnumerable<OwnedPixieData> GetAllPixies() => pixieDict.Values;
    public event Action OnPixieInventoryChanged;
    public int goldId = 0;


    //구조체 리스트는 저장 할수 있다고 이해해서 이렇게 만?들어놨는데 이상하다 싶으시면 지우셔도 됩니다 - 이동현
    public List<PixieSaveData> GetSaveList()
    {
        List<PixieSaveData> saveList = new List<PixieSaveData>();
        foreach (var pixie in pixieDict.Values)
        {
            saveList.Add(new PixieSaveData(pixie.pixieId, pixie.level));
        }
        return saveList;
    }

    public void LoadFromList(List<PixieSaveData> saveList)
    {
        pixieDict.Clear();
        if (saveList == null) return;

        foreach (var saveData in saveList)
        {
            pixieDict[saveData.pixieId] = new OwnedPixieData(saveData);
        }
    }

    #region IInventoryModule //픽시 조각은 StackItemInventoryModule에서 관리하도록 유지.
    public bool CanHandle(ItemType itemType)
    {
        // 추후 픽시조각->픽시가 아니라 직접 추가하는 기능이 생긴다면, pixie Enum추가해서 여기서 검사
        return false; 
    }
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        // 추후 픽시 직접 추가하는 기능이 생긴다면, pixie Enum후 여기서 추가로직
        return false;
    }

    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        return false;
    }
    public BigDouble GetAmount(InventoryItemContext item)
    {
        return BigDouble.Zero;
    }
    #endregion

    public bool TryUnlockPixie(int fairyId)
    {
        if (pixieDict.ContainsKey(fairyId)) return false;
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(fairyId, out var fairyData))return false;
        int fragmentId = fairyData.fragmentItemID;
        int requiredCost = 50; // 나중에 const 클래스나 데이터 테이블로 관리하도록 변경
        if (!InventoryManager.Instance.RemoveItem(fragmentId, requiredCost)) return false;

        pixieDict[fairyId] = new OwnedPixieData( new PixieSaveData(fairyId, 1));
        OnPixieInventoryChanged?.Invoke();
        return true;
    }
    public bool TryEvolvePixie(int fairyId)
    {
        if (!pixieDict.TryGetValue(fairyId, out var pixie)) return false;
        var table = pixie.fairyTable;
        if (!pixie.CanEvolve()) return false;
        if (table.nextID == 0) return false; 
        
        pixieDict.Remove(fairyId);
        int nextId = table.nextID;

        pixie.pixieId = nextId;
        pixie.level = 1; 
        
        pixie.TryGetTables(); 
        pixieDict[nextId] = pixie;

        OnPixieInventoryChanged?.Invoke();

        return true;
    }

    public bool TryLevelUpPixie(int fairyId)
    {
        if (!pixieDict.TryGetValue(fairyId, out var pixie)) return false;
        if (pixie.IsMaxLevel) return false;
        var goldCost = pixie.GetLevelUpCost();
        var fragCost = pixie.GetFragmentCost();
        bool isMythic = pixie.gradeTable.gradeName == "Mythic";

        SetGoldID();
        if (!InventoryManager.Instance.HasEnoughItem(goldId, goldCost)) return false;
        if (isMythic && !InventoryManager.Instance.HasEnoughItem(pixie.fairyTable.fragmentItemID, fragCost)) return false;

        InventoryManager.Instance.RemoveItem(goldId, goldCost);
        if (isMythic) InventoryManager.Instance.RemoveItem(pixie.fairyTable.fragmentItemID, fragCost);

        pixie.ExecuteLevelUp();

        OnPixieInventoryChanged?.Invoke();
        return true;
    }


    public void SetGoldID()
    {

        if (goldId != 0)
            return;
        else
        {
            foreach (var item in DataManager.Instance.ItemInfoDict)
            {
                if (item.Value.itemType == ItemType.FreeCurrency)
                {
                    goldId = item.Key;
                    break;
                }
            }
        }
    }
}
