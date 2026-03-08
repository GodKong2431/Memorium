using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ActiveSkillContents 전용 UI 컨트롤러.
/// NotEnough -> Enough -> Upgrade 흐름을 UI 상태로 렌더링한다.
/// </summary>
public class ActiveSkillUIController : UIControllerBase
{
    [Header("List Binding")]
    [SerializeField] private RectTransform listRoot;
    [SerializeField] private GameObject itemPrefab;
    [SerializeField] private bool clearListOnBuild = true;
    [SerializeField] private Button mergeAllButton;

    [Header("State Rule")]
    [SerializeField] private int unlockScrollCost = 3;
    [SerializeField] private float lockedItemHeight = 186f;
    [SerializeField] private float upgradeItemHeight = 283.2f;
    [SerializeField] private bool sortSkillIds = true;

    private readonly Dictionary<int, ActiveSkillItemView> itemViews = new Dictionary<int, ActiveSkillItemView>();
    private readonly Dictionary<int, Sprite> iconCache = new Dictionary<int, Sprite>();

    private SkillInventoryModule subscribedSkillModule;
    private Coroutine waitReadyRoutine;
    private bool built;

    protected override void Initialize()
    {
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        StartWaitReadyRoutine();
    }

    protected override void OnDisable()
    {
        if (waitReadyRoutine != null)
        {
            StopCoroutine(waitReadyRoutine);
            waitReadyRoutine = null;
        }

        base.OnDisable();
    }

    protected override void Subscribe()
    {
        EnsureSkillModuleSubscribed();
        BindMergeAllButton();
    }

    protected override void Unsubscribe()
    {
        UnbindMergeAllButton();
        UnsubscribeFromSkillModule();
    }

    protected override void RefreshView()
    {
        EnsureSkillModuleSubscribed();

        if (!IsReady())
            return;

        BuildIfNeeded();
        RenderAllItems();
    }

    private void BuildIfNeeded()
    {
        if (built)
            return;

        List<int> skillIds = GetDisplaySkillIds();
        if (skillIds.Count == 0)
            return;

        ActiveSkillItemListBuilder listBuilder = new ActiveSkillItemListBuilder(listRoot, itemPrefab, clearListOnBuild);
        Dictionary<int, ActiveSkillItemView> builtViews = listBuilder.BuildItems(
            skillIds,
            OnClickMergeSkill,
            lockedItemHeight,
            upgradeItemHeight);

        if (builtViews.Count == 0)
            return;

        itemViews.Clear();
        foreach (KeyValuePair<int, ActiveSkillItemView> pair in builtViews)
            itemViews[pair.Key] = pair.Value;

        built = true;
    }

    private void RenderAllItems()
    {
        if (!built)
            return;

        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return;

        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        foreach (KeyValuePair<int, ActiveSkillItemView> pair in itemViews)
        {
            int skillId = pair.Key;
            if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillId, out SkillInfoTable table))
                continue;

            OwnedSkillData ownedData = skillModule.GetSkillData(skillId);
            int scrollCount = ownedData != null ? ownedData.GetCount(SkillGrade.Scroll) : 0;
            int level = ownedData != null ? ownedData.level : 0;
            ActiveSkillItemVisualState state = ResolveState(ownedData, scrollCount);
            ResolveCountData(ownedData, state, scrollCount, out int currentCount, out int requiredCount, out bool canClickAction);
            Sprite icon = GetSkillIcon(skillId, table.skillIcon);

            pair.Value.Render(new ActiveSkillItemRenderData(
                table.skillName,
                icon,
                currentCount,
                requiredCount,
                level,
                canClickAction,
                state));
        }
    }

    private void OnClickMergeSkill(int skillId)
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        OwnedSkillData ownedData = skillModule.GetSkillData(skillId);
        int scrollCount = ownedData != null ? ownedData.GetCount(SkillGrade.Scroll) : 0;
        ActiveSkillItemVisualState state = ResolveState(ownedData, scrollCount);
        if (!CanClickAction(ownedData, state, scrollCount))
            return;

        skillModule.MergeChain(skillId);
        RenderAllItems();
    }

    private void OnClickMergeAllSkills()
    {
        SkillInventoryModule skillModule = GetSkillModule();
        if (skillModule == null)
            return;

        skillModule.MergeAllSkills();
        RenderAllItems();
    }

    private void ResolveCountData(
        OwnedSkillData ownedData,
        ActiveSkillItemVisualState state,
        int scrollCount,
        out int currentCount,
        out int requiredCount,
        out bool canClickAction)
    {
        requiredCount = unlockScrollCost;
        currentCount = scrollCount;
        canClickAction = CanClickAction(ownedData, state, scrollCount);
    }

    private bool CanClickAction(OwnedSkillData ownedData, ActiveSkillItemVisualState state, int scrollCount)
    {
        if (state == ActiveSkillItemVisualState.Enough)
            return scrollCount >= unlockScrollCost;

        if (state != ActiveSkillItemVisualState.Upgrade || ownedData == null)
            return false;

        if (ownedData.HighestGrade >= SkillGrade.Mythic)
            return false;

        return scrollCount >= unlockScrollCost;
    }

    private ActiveSkillItemVisualState ResolveState(OwnedSkillData ownedData, int scrollCount)
    {
        if (ownedData != null && ownedData.IsEquippable)
            return ActiveSkillItemVisualState.Upgrade;

        if (scrollCount >= unlockScrollCost)
            return ActiveSkillItemVisualState.Enough;

        return ActiveSkillItemVisualState.NotEnough;
    }

    private List<int> GetDisplaySkillIds()
    {
        List<int> skillIds = new List<int>();

        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return skillIds;

        foreach (int skillId in DataManager.Instance.SkillInfoDict.Keys)
            skillIds.Add(skillId);

        if (sortSkillIds)
            skillIds.Sort((a, b) => a.CompareTo(b));

        return skillIds;
    }

    private void EnsureSkillModuleSubscribed()
    {
        SkillInventoryModule module = GetSkillModule();
        if (module == null)
            return;

        if (subscribedSkillModule == module)
            return;

        UnsubscribeFromSkillModule();
        subscribedSkillModule = module;
        subscribedSkillModule.OnInventoryChanged += OnSkillInventoryChanged;
    }

    private void BindMergeAllButton()
    {
        mergeAllButton.onClick.RemoveListener(OnClickMergeAllSkills);
        mergeAllButton.onClick.AddListener(OnClickMergeAllSkills);
    }

    private void UnbindMergeAllButton()
    {
        mergeAllButton.onClick.RemoveListener(OnClickMergeAllSkills);
    }

    private void UnsubscribeFromSkillModule()
    {
        if (subscribedSkillModule == null)
            return;

        subscribedSkillModule.OnInventoryChanged -= OnSkillInventoryChanged;
        subscribedSkillModule = null;
    }

    private void OnSkillInventoryChanged()
    {
        if (!built)
        {
            RefreshView();
            return;
        }

        RenderAllItems();
    }

    private SkillInventoryModule GetSkillModule()
    {
        return InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
    }

    private bool IsReady()
    {
        return listRoot != null
            && itemPrefab != null
            && DataManager.Instance != null
            && DataManager.Instance.DataLoad
            && DataManager.Instance.SkillInfoDict != null
            && DataManager.Instance.SkillInfoDict.Count > 0
            && GetSkillModule() != null;
    }

    private void StartWaitReadyRoutine()
    {
        if (waitReadyRoutine != null)
            StopCoroutine(waitReadyRoutine);

        waitReadyRoutine = StartCoroutine(CoWaitUntilReady());
    }

    private IEnumerator CoWaitUntilReady()
    {
        yield return new WaitUntil(IsReady);
        RefreshView();
        waitReadyRoutine = null;
    }

    private Sprite GetSkillIcon(int skillId, string skillIconKey)
    {
        if (iconCache.TryGetValue(skillId, out Sprite cached))
            return cached;

        Sprite loaded = TryLoadSprite(skillIconKey);
        iconCache[skillId] = loaded;
        return loaded;
    }

    private static Sprite TryLoadSprite(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        string path = key.Trim();

        Sprite sprite = Resources.Load<Sprite>(path);
        if (sprite != null)
            return sprite;

        int dotIndex = path.LastIndexOf(".", StringComparison.Ordinal);
        if (dotIndex > 0)
        {
            string withoutExtension = path.Substring(0, dotIndex);
            sprite = Resources.Load<Sprite>(withoutExtension);
            if (sprite != null)
                return sprite;
        }

        const string resourcesToken = "Resources/";
        int resourcesIndex = path.IndexOf(resourcesToken, StringComparison.OrdinalIgnoreCase);
        if (resourcesIndex < 0)
            return null;

        string relativePath = path.Substring(resourcesIndex + resourcesToken.Length);
        int relativeDotIndex = relativePath.LastIndexOf(".", StringComparison.Ordinal);
        if (relativeDotIndex > 0)
            relativePath = relativePath.Substring(0, relativeDotIndex);

        return Resources.Load<Sprite>(relativePath);
    }

    private void OnValidate()
    {
        if (unlockScrollCost < 1)
            unlockScrollCost = 1;

        if (lockedItemHeight < 1f)
            lockedItemHeight = 1f;

        if (upgradeItemHeight < 1f)
            upgradeItemHeight = 1f;
    }
}
