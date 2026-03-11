
using System;
using System.Collections.Generic;


public sealed class PixieInventoryModule : IInventoryModule
{
    private readonly Dictionary<int, OwnedPixieData> pixieDict = new Dictionary<int, OwnedPixieData>();
    public IEnumerable<OwnedPixieData> GetAllPixies() => pixieDict.Values;
    public int goldId = 0;
    private int equippedPixieId  = 0;
    public int EquippedPixiedID() => equippedPixieId;

    public event Action OnPixieInventoryChanged;
    public event Action<OwnedPixieData> OnPixieEquipped;
    //구조체 리스트는 저장 할 수 있다고 이해해서 이렇게 만?들어놨는데 이상하다 싶으시면 지우셔도 됩니다
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
        return itemType == ItemType.Pixie;
    }
    public bool TryAdd(InventoryItemContext item, BigDouble amount)
    {
        if (!CanHandle(item.ItemType)) return false;

        int addCount = (int)Math.Floor(amount.ToDouble());
        if (addCount <= 0) return false;

        int pixieId = item.ItemId;
        bool isChanged = false;

        for (int i = 0; i < addCount; i++)
        {
            if (pixieDict.ContainsKey(pixieId))
            {
                //중복 획득? 조각으로 변환?
            }
            else
            {
                pixieDict[pixieId] = new OwnedPixieData(new PixieSaveData(pixieId, 1));
                isChanged = true;
            }
        }

        if (isChanged)
        {
            OnPixieInventoryChanged?.Invoke();
        }
        return true;
    }

    public bool TryRemove(InventoryItemContext item, BigDouble amount)
    {
        return false;
    }
    public BigDouble GetAmount(InventoryItemContext item)
    {
        if (!CanHandle(item.ItemType)) return BigDouble.Zero;
        return BigDouble.Zero;
    }
    #endregion

    public void EquipPixie(int fairyId)
    {
        if (!pixieDict.TryGetValue(fairyId, out var pixie)) return;

        equippedPixieId = fairyId;
        OnPixieEquipped?.Invoke(pixie);
    }
    public void UnequipPixie()
    {
        equippedPixieId = 0;
        OnPixieEquipped?.Invoke(null); // null을 보내서 소환 해제 알림
    }
    public OwnedPixieData GetOwnedPixieData(int pixieId)
    {
        if (!pixieDict.TryGetValue(pixieId, out var pixie)) return null;
        return pixie;
    }
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
        
        pixie.TryGetData(); 
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
        bool isMythic = pixie.gradeTable.fairyGrade == FairyGrade.Mythic;

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
