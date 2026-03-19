using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

// 단일 장비 탭의 티어/아이템 UI를 생성하고 갱신한다.
public class EquipTabUIController : UIControllerBase
{
    // 이 탭이 표시할 장비 타입이다.
    [SerializeField] private EquipmentType tabType = EquipmentType.Weapon;
    // 리스트 아이템 클릭 시 열 강화 패널 컨트롤러다.
    [SerializeField] private EquipReinforceUIController reinforceController;
    // 티어 그룹을 생성할 루트 트랜스폼이다.
    [SerializeField] private RectTransform root;
    // 티어 그룹 프리팹이다.
    [SerializeField] private GameObject tierPrefab;
    // 장비 아이템 셀 프리팹이다.
    [SerializeField] private GameObject itemPrefab;
    // 티어 별 아이콘 프리팹이다.
    [SerializeField] private GameObject starPrefab;
    // 1회 합성에 필요한 아이템 개수다.
    [SerializeField] private int mergeCount = 3;
    // 각 아이템에 표시할 임시 레벨 텍스트다.
    [SerializeField] private string levelText = "Lv. 0";

    // 티어 패널을 투명 처리할 때 사용하는 색상이다.
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
    // 아이템 ID 기준으로 생성된 뷰를 보관한다.
    private readonly Dictionary<int, EquipItemView> views = new Dictionary<int, EquipItemView>();

    // 인벤토리 매니저 캐시 참조다.
    private InventoryManager inventory;
    // 장비 인벤토리 모듈 캐시 참조다.
    private EquipmentInventoryModule equipModule;
    // UI 목록이 1회 생성되었는지 여부다.
    private bool isBuilt;

    // 데이터 준비 전까지 빌드를 시도한다.
    private void Update()
    {
        if (!isBuilt)
            RefreshView();
    }

    // 새로고침 이벤트와 인벤토리 이벤트를 구독한다.
    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        BindInventory();
    }

    // 등록한 이벤트 구독을 모두 해제한다.
    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        UnbindInventory();
    }

    // 생성 상태와 아이템 상태를 한 번에 갱신한다.
    protected override void RefreshView()
    {
        if (!TryPrepare())
            return;

        if (!isBuilt)
            BuildViews();

        RefreshAllItems();
    }

    // 필수 참조와 런타임 모듈 준비 상태를 검사한다.
    private bool TryPrepare()
    {
        if (root == null || tierPrefab == null || itemPrefab == null || starPrefab == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (!BindInventory())
            return false;

        equipModule = inventory.GetModule<EquipmentInventoryModule>();
        return equipModule != null && equipModule.IsInitialized;
    }

    // 인벤토리 인스턴스 변경 시 이벤트를 다시 바인딩한다.
    private bool BindInventory()
    {
        InventoryManager current = InventoryManager.Instance;
        if (current == null)
            return false;

        if (inventory == current)
            return true;

        UnbindInventory();
        inventory = current;
        inventory.OnItemAmountChanged += HandleAmountChanged;
        return true;
    }

    // 인벤토리 이벤트 바인딩을 해제하고 참조를 정리한다.
    private void UnbindInventory()
    {
        if (inventory == null)
            return;

        inventory.OnItemAmountChanged -= HandleAmountChanged;
        inventory = null;
    }

    // 아이템 수량 변경 시 해당 셀의 개수/잠금 상태를 갱신한다.
    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (!views.TryGetValue(item.ItemId, out EquipItemView view))
            return;

        view.RenderCount(ToCount(amount), mergeCount);

        if (equipModule != null)
            view.SetDimmed(!equipModule.IsUnlocked(item.ItemId));
    }

    // 현재 탭의 티어 그룹과 아이템 셀을 모두 생성한다.
    private void BuildViews()
    {
        views.Clear();
        ClearRoot();

        List<EquipListTable> tables = CollectTables();
        int currentTier = int.MinValue;
        int orderInTier = 0;
        EquipTierUI currentTierUI = null;

        for (int i = 0; i < tables.Count; i++)
        {
            EquipListTable table = tables[i];
            int tier = Mathf.Max(1, table.grade);

            if (tier != currentTier)
            {
                currentTier = tier;
                orderInTier = 0;
                currentTierUI = CreateTier(tier);
            }

            if (currentTierUI != null)
                CreateItem(currentTierUI, table, tier, orderInTier++);
        }

        isBuilt = true;
    }

    // 티어 헤더 오브젝트를 만들고 별 UI를 구성한다.
    private EquipTierUI CreateTier(int tier)
    {
        GameObject tierObject = Instantiate(tierPrefab, root, false);
        tierObject.name = $"Tier_{tier:00}";

        EquipTierUI tierUI = tierObject.GetComponent<EquipTierUI>();
        if (tierUI == null)
            return null;

        tierUI.TierPanel.color = Transparent;

        Color tierColor = RarityColor.TierColorByTier(tier);
        int starCount = GetStarCount(tier);

        for (int i = 0; i < starCount; i++)
        {
            GameObject starObject = Instantiate(starPrefab, tierUI.TierRoot, false);
            starObject.name = $"(Img)TierStar_{i + 1}";

            Image star = starObject.GetComponent<Image>();
            if (star != null)
                star.color = tierColor;
        }

        return tierUI;
    }

    // 아이템 셀 하나를 생성하고 런타임 뷰에 등록한다.
    private void CreateItem(EquipTierUI tierUI, EquipListTable table, int tier, int orderInTier)
    {
        GameObject itemObject = Instantiate(itemPrefab, tierUI.ListRoot, false);
        itemObject.name = $"Equipment_{table.ID}";

        EquipItemUI itemUI = itemObject.GetComponent<EquipItemUI>();
        if (itemUI == null)
            return;

        EquipItemView view = new EquipItemView(itemUI);
        int itemId = table.ID;
        //레벨 텍스트 수정
        //아이템 아이디 기반으로 레벨 가져오기 
        EquipmentInventoryModule equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
        EquipmentData equipmentData = equipmentModule.GetEquipment(itemId);
        if (equipmentData.equipmentId == itemId)
        {
            levelText = "Lv. " + equipmentData.equipmentReinforcement;
        }
        else
        {
            levelText = "Lv. 0";
        }

        view.Bind(() => ClickItem(itemId));
        view.Render(
            LoadIcon(table),
            levelText,
            GetStarCount(tier),
            RarityColor.TierColorByTier(tier));
        view.SetFrameColor(RarityColor.ColorByOrderIndex(orderInTier));
        view.SetDimmed(!equipModule.IsUnlocked(itemId));
        view.RenderCount(ToCount(inventory.GetItemAmount(itemId)), mergeCount);

        views[itemId] = view;
    }


    // 생성된 모든 아이템 셀을 최신 데이터로 갱신한다.
    private void RefreshAllItems()
    {
        if (inventory == null || equipModule == null)
            return;

        foreach (KeyValuePair<int, EquipItemView> pair in views)
        {
            int itemId = pair.Key;
            EquipItemView view = pair.Value;

            view.RenderCount(ToCount(inventory.GetItemAmount(itemId)), mergeCount);
            view.SetDimmed(!equipModule.IsUnlocked(itemId));
            //아이템 아이디 기반으로 레벨 가져오기 
            EquipmentInventoryModule equipmentModule = InventoryManager.Instance.GetModule<EquipmentInventoryModule>();
            EquipmentData equipmentData = equipmentModule.GetEquipment(itemId);
            if (equipmentData.equipmentId == itemId)
            {
                levelText = "Lv. " + equipmentData.equipmentReinforcement;
            }
            else
            {
                levelText = "Lv. 0";
            }
            view.RenderLevel(levelText);

        }
    }

    // 이 탭에서 사용할 장비 테이블 행을 수집하고 정렬한다.
    private List<EquipListTable> CollectTables()
    {
        List<EquipListTable> tables = new List<EquipListTable>();

        foreach (KeyValuePair<int, EquipListTable> pair in DataManager.Instance.EquipListDict)
        {
            EquipListTable table = pair.Value;
            if (table.equipmentType == tabType)
                tables.Add(table);
        }

        tables.Sort((lhs, rhs) =>
        {
            int tierCompare = lhs.grade.CompareTo(rhs.grade);
            if (tierCompare != 0)
                return tierCompare;

            int rarityCompare = lhs.rarityType.CompareTo(rhs.rarityType);
            if (rarityCompare != 0)
                return rarityCompare;

            return lhs.equipmentTier.CompareTo(rhs.equipmentTier);
        });

        return tables;
    }

    // 루트 하위에 생성된 티어/아이템 오브젝트를 모두 삭제한다.
    private void ClearRoot()
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    // 장비 테이블 정보로 아이콘 스프라이트를 로드한다.
    private static Sprite LoadIcon(EquipListTable table)
    {
        string key = string.IsNullOrEmpty(table.iconResource)
            ? table.equipmentName
            : table.iconResource;

        return EquipmentIconResolver.LoadSprite(key);
    }

    // 장비 등급 티어를 1~5 별 개수로 변환한다.
    private static int GetStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
    }

    // BigDouble 수량을 0 이상 int 값으로 안전하게 변환한다.
    private static int ToCount(BigDouble amount)
    {
        if (amount <= BigDouble.Zero)
            return 0;

        double value = amount.ToDouble();
        if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0d)
            return 0;

        return value >= int.MaxValue ? int.MaxValue : (int)value;
    }

    // 장비 아이템 셀 클릭 이벤트를 처리한다.
    private void ClickItem(int itemId)
    {
        if (reinforceController == null || itemId == 0)
            return;

        reinforceController.Show(itemId);
    }
}
