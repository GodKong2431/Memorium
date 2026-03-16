
using System;
using System.Collections.Generic;


public sealed class PixieInventoryModule : IInventoryModule
{
    private readonly Dictionary<int, OwnedPixieData> pixieDict = new Dictionary<int, OwnedPixieData>();
    
    public int goldId = 0;

    const int UNLOCK_COST = 50;
    private int equippedPixieId  = 0;
    public int EquippedPixiedID() => equippedPixieId;

    public event Action OnPixieInventoryChanged;

    public event Action<OwnedPixieData> OnPixieEquipped;

    //구조체 리스트는 저장 할 수 있다고 이해해서 이렇게 만?들어놨는데 이상하다 싶으시면 다른 방식으로 하셔도 괜찮습니다.

    public List<PixieSaveData> saveList = new List<PixieSaveData>();

    /// <summary>
    /// 가지고 있는 모든 Pixie의 id와 레벨을 담은 구조체 리스트 반환, UI랑 저장에 쓰시면 될?듯
    /// </summary>
    /// <returns></returns>
    public List<PixieSaveData> GetSaveList()
    {
        saveList.Clear();
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

    // TO DO: ItemType.Pixie로 받게했는데. 현재 드랍테이블에는 Pixie관련 enum이없어서 InventoryMamanger에서 변환해주지 않을것같습니다.
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
    

    #region UI 표시용
    /// <summary>
    /// 특정 픽시 보유 여부 반환
    /// </summary>
    public bool IsOwned(int fairyId)
    {
        return pixieDict.ContainsKey(fairyId);
    }

    /// <summary>
    /// 보유 픽시 데이터 목록 반환
    /// </summary>
    public IEnumerable<OwnedPixieData> GetAllPixies()
    {
        return pixieDict.Values;
    }

    /// <summary>
    /// 보유/미보유 포함  전체 픽시 ID 목록 반환
    /// </summary>
    public IEnumerable<int> GetAllFairyIDsInGame()
    {
        return DataManager.Instance.FairyInfoDict.Keys;
    }
    public OwnedPixieData GetOwnedPixieData(int pixieId)
    {
        if (!pixieDict.TryGetValue(pixieId, out var pixie)) return null;
        return pixie;
    }
    /// <summary>
    /// 해당 픽시 매칭된 픽시조각 ID 갯수 확인하여 해금 가능 여부 반환
    /// </summary>
    public bool CanUnlockPixie(int fairyId)
    {
        if (pixieDict.ContainsKey(fairyId)) return false;
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(fairyId, out var data)) return false;

        return InventoryManager.Instance.HasEnoughItem(data.fragmentItemID, UNLOCK_COST);
    }
    /// <summary>
    /// 골드 보유량 및 픽시 등급, 레벨 체크하여 레벨업 가능 여부 반환
    /// </summary>
    public bool CanLevelUp(int fairyId)
    {
        if (!pixieDict.TryGetValue(fairyId, out var pixie)) return false;
        if (pixie.IsMaxLevel()) return false;

        SetGoldID();
        bool hasGold = InventoryManager.Instance.HasEnoughItem(goldId, pixie.GetLevelUpCost());

        if (pixie.gradeTable.fairyGrade == FairyGrade.Mythic)
        {
            bool hasFrag = InventoryManager.Instance.HasEnoughItem(pixie.fairyTable.fragmentItemID, pixie.GetFragmentCost());
            return hasGold && hasFrag;
        }
        return hasGold;
    }

    /// <summary>
    /// 성장 가능 여부 반환
    /// </summary>
    public bool CanEvolve(int fairyId)
    {
        if (!pixieDict.TryGetValue(fairyId, out var pixie)) return false;
        return pixie.CanEvolve() && pixie.fairyTable.nextID != 0;
    }

    /// <summary>
    /// 픽시 ID로 매칭된 픽시조각 갯수반환
    /// </summary>
    public BigDouble GetPixiePieceFromPixieID(int fairyID)
    {
        if (!DataManager.Instance.FairyInfoDict.TryGetValue(fairyID, out var fairyData)) return BigDouble.Zero;
        return InventoryManager.Instance.GetItemAmount(fairyData.fragmentItemID);
    }

    #endregion

    #region UI 버튼용

    /// <summary>
    /// 해당 픽시 장착 및 소환
    /// </summary>
    public void EquipPixie(int fairyId)
    {
        if (!pixieDict.TryGetValue(fairyId, out var pixie)) return;

        equippedPixieId = fairyId;
        OnPixieEquipped?.Invoke(pixie);
    }
    /// <summary>
    /// 픽시 장착 해제
    /// </summary>
    public void UnequipPixie()
    {
        equippedPixieId = 0;
        OnPixieEquipped?.Invoke(null);
    }

    // TO DO : 만약 종류마다 텍스트 다르게 해야한다면 enum값을 반환하게해서 메세지를 다르게하거나
    // 아니면 스트링 자체를 반환하게해서 그대로 띄우게 해도 괜찮을 것 같습니다.

    /// <summary>
    /// 모든 업그레이드(해금/레벨업/성장) 호출 , 하나라도 성공시 종료 => true 반환
    /// </summary>
    public bool TryUpgradePixie(int fairyId)
    {
        if (TryEvolvePixie(fairyId)) return true;
        if (TryLevelUpPixie(fairyId)) return true;
        if (TryUnlockPixie(fairyId)) return true;
        return false;
    }
    public bool TryUnlockPixie(int fairyId)
    {
        if (!CanUnlockPixie(fairyId)) return false;

        var data = DataManager.Instance.FairyInfoDict[fairyId];
        InventoryManager.Instance.RemoveItem(data.fragmentItemID, UNLOCK_COST);

        pixieDict[fairyId] = new OwnedPixieData(new PixieSaveData(fairyId, 1));
        OnPixieInventoryChanged?.Invoke();
        return true;
    }
    public bool TryEvolvePixie(int fairyId)
    {
        if (!CanEvolve(fairyId)) return false;

        var pixie = pixieDict[fairyId];
        int nextId = pixie.fairyTable.nextID;

        pixieDict.Remove(fairyId);

        pixie.pixieId = nextId;
        pixie.level = 1;
        pixie.TryGetData();
        pixieDict[nextId] = pixie;

        OnPixieInventoryChanged?.Invoke();
        return true;
    }

    public bool TryLevelUpPixie(int fairyId)
    {
        if (!CanLevelUp(fairyId)) return false;

        var pixie = pixieDict[fairyId];
        InventoryManager.Instance.RemoveItem(goldId, pixie.GetLevelUpCost());

        if (pixie.gradeTable.fairyGrade == FairyGrade.Mythic)
        {
            InventoryManager.Instance.RemoveItem(pixie.fairyTable.fragmentItemID, pixie.GetFragmentCost());
        }

        pixie.ExecuteLevelUp();
        OnPixieInventoryChanged?.Invoke();
        return true;
    }
    #endregion

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
