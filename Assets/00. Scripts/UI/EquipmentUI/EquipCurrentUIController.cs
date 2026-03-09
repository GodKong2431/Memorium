using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 현재 장착 장비 UI와 자동 합성/장착 버튼 상태를 갱신한다.
public class EquipCurrentUIController : UIControllerBase
{
    // 합성/장착 도메인 액션을 호출하는 핸들러다.
    [SerializeField] private EquipmentHandler handler;
    // 자동 합성 실행 버튼이다.
    [SerializeField] private Button mergeButton;
    // 자동 장착 실행 버튼이다.
    [SerializeField] private Button equipButton;
    // 현재 장착 셀을 생성할 루트 트랜스폼이다.
    [SerializeField] private RectTransform root;
    // 슬롯별 셀 생성에 사용할 아이템 프리팹이다.
    [SerializeField] private GameObject itemPrefab;
    // 각 장착 아이템에 표시할 임시 레벨 텍스트다.
    [SerializeField] private string levelText = "Lv. 0";

    // 장착 슬롯 표시 순서다.
    private static readonly EquipmentType[] Order =
    {
        EquipmentType.Weapon,
        EquipmentType.Helmet,
        EquipmentType.Armor,
        EquipmentType.Glove,
        EquipmentType.Boots
    };

    // 생성된 슬롯 아이템 뷰 목록이다.
    private readonly List<EquipItemView> views = new List<EquipItemView>();

    // 인벤토리 매니저 캐시 참조다.
    private InventoryManager inventory;
    // 슬롯 UI가 1회 생성되었는지 여부다.
    private bool isBuilt;

    // 데이터 준비 전까지 빌드를 시도한다.
    private void Update()
    {
        if (!isBuilt)
            RefreshView();
    }

    // 새로고침/장착/인벤토리 이벤트와 버튼 클릭을 구독한다.
    protected override void Subscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested += RefreshView;
        PlayerEquipment.EquippedItemChanged += HandleEquippedChanged;

        if (mergeButton != null)
            mergeButton.onClick.AddListener(ClickMerge);

        if (equipButton != null)
            equipButton.onClick.AddListener(ClickEquip);

        BindInventory();
    }

    // 등록한 이벤트와 클릭 핸들러를 모두 해제한다.
    protected override void Unsubscribe()
    {
        EquipmentHandler.EquipmentUiRefreshRequested -= RefreshView;
        PlayerEquipment.EquippedItemChanged -= HandleEquippedChanged;

        if (mergeButton != null)
            mergeButton.onClick.RemoveListener(ClickMerge);

        if (equipButton != null)
            equipButton.onClick.RemoveListener(ClickEquip);

        UnbindInventory();
    }

    // 현재 장착 슬롯 UI와 버튼 상태를 갱신한다.
    protected override void RefreshView()
    {
        if (!TryPrepare())
            return;

        if (!isBuilt)
            BuildViews();

        RefreshButtons();
        RefreshCurrent();
    }

    // 렌더링에 필요한 참조와 런타임 모듈 상태를 검사한다.
    private bool TryPrepare()
    {
        if (handler == null || !handler.dataLoad)
            return false;

        if (root == null || itemPrefab == null)
            return false;

        if (DataManager.Instance == null || !DataManager.Instance.DataLoad || DataManager.Instance.EquipListDict == null)
            return false;

        if (!BindInventory())
            return false;

        EquipmentInventoryModule module = inventory.GetModule<EquipmentInventoryModule>();
        return module != null && module.IsInitialized;
    }

    // 필요 시 인벤토리 수량 변경 이벤트를 바인딩한다.
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

    // 인벤토리 이벤트 바인딩을 해제하고 캐시를 정리한다.
    private void UnbindInventory()
    {
        if (inventory == null)
            return;

        inventory.OnItemAmountChanged -= HandleAmountChanged;
        inventory = null;
    }

    // 장비 슬롯 개수만큼 비상호작용 아이템 뷰를 생성한다.
    private void BuildViews()
    {
        views.Clear();

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);

        for (int i = 0; i < Order.Length; i++)
        {
            GameObject go = Instantiate(itemPrefab, root, false);
            go.name = $"CurrentEquipment_{Order[i]}";

            EquipItemUI ui = go.GetComponent<EquipItemUI>();
            if (ui == null)
                continue;

            ui.Button.onClick.RemoveAllListeners();
            ui.Button.interactable = false;
            ui.MergeSlider.gameObject.SetActive(false);

            views.Add(new EquipItemView(ui));
        }

        isBuilt = true;
    }

    // 도메인 조건에 따라 자동 합성/장착 버튼 상태를 갱신한다.
    private void RefreshButtons()
    {
        if (mergeButton != null)
            mergeButton.interactable = handler.CanAutoMerge();

        if (equipButton != null)
            equipButton.interactable = handler.CanAutoEquip();
    }

    // 플레이어 장착 데이터 기준으로 슬롯 UI를 갱신한다.
    private void RefreshCurrent()
    {
        if (!handler.TryGetPlayerEquipment(out PlayerEquipment player))
            return;

        if (views.Count < Order.Length)
            return;

        for (int i = 0; i < Order.Length; i++)
        {
            int itemId = player.ReturnItemNum(Order[i]);
            if (!DataManager.Instance.EquipListDict.TryGetValue(itemId, out EquipListTable info))
                continue;

            Sprite icon = LoadIcon(info);
            int starCount = GetStarCount(info.grade);

            views[i].Render(icon, levelText, starCount, RarityColor.TierColorByTier(info.grade));
            views[i].SetFrameColor(RarityColor.ItemGradeColor(info.rarityType));
        }
    }

    // 자동 합성을 실행하고 버튼 상태를 다시 갱신한다.
    private void ClickMerge()
    {
        handler.TryAutoMerge();
        RefreshButtons();
    }

    // 자동 장착을 실행하고 버튼 상태를 다시 갱신한다.
    private void ClickEquip()
    {
        handler.TryAutoEquip();
        RefreshButtons();
    }

    // 장착 변경 이벤트 발생 시 슬롯 UI를 갱신한다.
    private void HandleEquippedChanged(EquipmentType type, int itemId)
    {
        RefreshCurrent();
    }

    // 장비 아이템 수량 변경 시 액션 버튼 상태를 갱신한다.
    private void HandleAmountChanged(InventoryItemContext item, BigDouble amount)
    {
        if (IsEquipmentItem(item.ItemType))
            RefreshButtons();
    }

    // 장비 테이블 정보로 아이콘 스프라이트를 로드한다.
    private static Sprite LoadIcon(EquipListTable table)
    {
        string key = string.IsNullOrEmpty(table.iconResource)
            ? table.equipmentName
            : table.iconResource;

        return string.IsNullOrEmpty(key) ? null : Resources.Load<Sprite>(key);
    }

    // 장비 등급 티어를 1~5 별 개수로 변환한다.
    private static int GetStarCount(int tier)
    {
        return ((Mathf.Max(1, tier) - 1) % 5) + 1;
    }

    // 아이템 타입이 장비 카테고리인지 판별한다.
    private static bool IsEquipmentItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.Weapon:
            case ItemType.Helmet:
            case ItemType.Armor:
            case ItemType.Glove:
            case ItemType.Boots:
                return true;
            default:
                return false;
        }
    }
}
