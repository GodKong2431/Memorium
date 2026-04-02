using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DungeonContentUI : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private TextMeshProUGUI dungeonNameText;

    [Header("Reward")]
    [SerializeField] private RectTransform rewardContentRoot;
    [SerializeField] private GameObject equipmentRewardItemPrefab;

    [Header("State")]
    [SerializeField] private GameObject contentLockPanel;
    [SerializeField] private Button contentLockButton;
    [SerializeField] private string ShowlockMessage= "해금되지 않은 콘텐츠입니다";
    [SerializeField] private Button enterButton;
    [SerializeField] private TextMeshProUGUI neededKeyText;
    private StageType stageType;

    public void SetEnterInteractable(bool interactable) { enterButton.interactable = interactable; }
    public void SetDungeonName(string dungeonName) { dungeonNameText.text = dungeonName; }
    public void SetDungeonType(StageType type) { stageType = type; }
    public void SetLocked(bool isLocked) { 
        contentLockPanel.SetActive(isLocked);
        FindUnLockStageAndSetMessage();
        if (contentLockButton == null)
            Debug.Log("[DungeonContentUI] 던전 콘텐츠 버튼이 없습니다");
        else
            Debug.Log("[DungeonContentUI] 던전 콘텐츠 버튼이 있습니다");
        contentLockButton.onClick.AddListener(() => InstanceMessageManager.TryShow(ShowlockMessage));
    }

    public void FindUnLockStageAndSetMessage()
    {
        int unLockStageId = -1;
        foreach (var dungeonreqTable in DataManager.Instance.DungeonReqDict)
        {
            if (dungeonreqTable.Value.stageType == stageType)
            {
                unLockStageId = dungeonreqTable.Value.stageID01;
                break;
            }
        }
        if(unLockStageId < 0 )
            return;
        StageManageTable unLockStage = DataManager.Instance.StageManageDict[unLockStageId];
        ShowlockMessage = "해금되지 않은 콘텐츠입니다\n해금 조건 : " +unLockStage.desc+" "+unLockStage.floorNumber+"-"+unLockStage.sceneNumber+ " 클리어";
    }

    public void SetNeededKeyState(BigDouble currentKey, int requiredKey, Color normalColor, Color notEnoughColor)
    {
        neededKeyText.text = requiredKey.ToString();
        neededKeyText.color = currentKey >= new BigDouble(requiredKey) ? normalColor : notEnoughColor;
    }

    public void BindEnter(UnityAction onClick)
    {
        enterButton.onClick.RemoveAllListeners();
        enterButton.onClick.AddListener(onClick);
        UiButtonSoundPlayer.Ensure(enterButton, UiSoundIds.DefaultButton);
    }

    public void RebuildRewards(
        IReadOnlyList<RewardManager.DungeonRewardEntry> rewards,
        Func<RewardManager.DungeonRewardEntry, Sprite> iconResolver,
        bool showAmounts)
    {
        RebuildRewardItems(rewardContentRoot, rewards, iconResolver, showAmounts, equipmentRewardItemPrefab);
    }

    internal static void RebuildRewardItems(
        RectTransform rewardRoot,
        IReadOnlyList<RewardManager.DungeonRewardEntry> rewards,
        Func<RewardManager.DungeonRewardEntry, Sprite> iconResolver,
        bool showAmounts,
        GameObject equipmentRewardItemPrefab = null)
    {
        if (rewardRoot == null)
            return;

        if (!rewardRoot.gameObject.activeSelf)
            rewardRoot.gameObject.SetActive(true);

        ClearRewardChildren(rewardRoot);

        int rewardCount = rewards != null ? rewards.Count : 0;
        for (int i = 0; i < rewardCount; i++)
        {
            RewardManager.DungeonRewardEntry reward = rewards[i];
            Sprite icon = iconResolver != null ? iconResolver(reward) : null;
            Transform rewardItem = CreateRewardItem(rewardRoot, reward, equipmentRewardItemPrefab);
            if (rewardItem == null)
                continue;

            rewardItem.name = IsActualEquipmentReward(reward)
                ? $"(Btn)EquipmentReward_{i + 1}"
                : $"(Item)Reward_{i + 1}";

            ApplyRewardVisualState(rewardItem, reward, icon, showAmounts);
        }

        RefreshRewardLayout(rewardRoot);
    }

    private static void ClearRewardChildren(RectTransform rewardRoot)
    {
        for (int i = rewardRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = rewardRoot.GetChild(i);
            if (child == null)
                continue;

            child.SetParent(null, false);
            UnityEngine.Object.Destroy(child.gameObject);
        }
    }

    private static Transform CreateRewardItem(
        RectTransform rewardRoot,
        RewardManager.DungeonRewardEntry reward,
        GameObject equipmentRewardItemPrefab)
    {
        if (equipmentRewardItemPrefab != null)
        {
            GameObject rewardItemObject = Instantiate(equipmentRewardItemPrefab, rewardRoot, false);
            rewardItemObject.SetActive(true);
            return rewardItemObject.transform;
        }

        return CreateDefaultRewardItem(rewardRoot).transform;
    }

    private static bool IsActualEquipmentReward(RewardManager.DungeonRewardEntry reward)
    {
        return reward.visualType == RewardManager.DungeonRewardVisualType.Equipment && reward.itemId > 0;
    }

    private static GameObject CreateDefaultRewardItem(RectTransform rewardRoot)
    {
        int layer = rewardRoot.gameObject.layer;
        GameObject rewardItemObject = new GameObject(
            "(Item)Reward",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(LayoutElement));
        rewardItemObject.layer = layer;
        rewardItemObject.transform.SetParent(rewardRoot, false);

        RectTransform rewardItemRect = rewardItemObject.GetComponent<RectTransform>();
        rewardItemRect.anchorMin = new Vector2(0.5f, 0.5f);
        rewardItemRect.anchorMax = new Vector2(0.5f, 0.5f);
        rewardItemRect.sizeDelta = new Vector2(120f, 120f);
        rewardItemRect.localScale = Vector3.one;

        Image backgroundImage = rewardItemObject.GetComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.18f);
        backgroundImage.raycastTarget = false;

        LayoutElement layoutElement = rewardItemObject.GetComponent<LayoutElement>();
        layoutElement.preferredWidth = 120f;
        layoutElement.preferredHeight = 120f;
        layoutElement.minWidth = 120f;
        layoutElement.minHeight = 120f;

        GameObject iconObject = new GameObject(
            "(Img)RewardIcon",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        iconObject.layer = layer;
        iconObject.transform.SetParent(rewardItemObject.transform, false);

        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0f, 0f);
        iconRect.anchorMax = new Vector2(1f, 1f);
        iconRect.offsetMin = new Vector2(8f, 8f);
        iconRect.offsetMax = new Vector2(-8f, -8f);
        iconRect.localScale = Vector3.one;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        TextMeshProUGUI amountText = CreateAmountText(rewardItemObject.transform, layer);
        DungeonRewardItemUI rewardItemUi = rewardItemObject.AddComponent<DungeonRewardItemUI>();
        rewardItemUi.Bind(iconImage, amountText);

        return rewardItemObject;
    }

    private static void ApplyRewardVisualState(
        Transform rewardItem,
        RewardManager.DungeonRewardEntry reward,
        Sprite icon,
        bool showAmounts)
    {
        if (rewardItem.TryGetComponent(out CachaResultItemUI rewardItemUi))
        {
            ApplyItemRewardVisualState(rewardItemUi, reward, icon, showAmounts);
            return;
        }

        if (rewardItem.TryGetComponent(out EquipItemUI equipmentItemUi))
        {
            ApplyEquipmentRewardVisualState(equipmentItemUi, reward, icon);
            return;
        }

        ApplyDefaultRewardVisualState(rewardItem, reward, icon, showAmounts);
    }

    private static void ApplyDefaultRewardVisualState(
        Transform rewardItem,
        RewardManager.DungeonRewardEntry reward,
        Sprite icon,
        bool showAmounts)
    {
        DungeonRewardItemUI rewardItemUi = rewardItem.GetComponent<DungeonRewardItemUI>();
        if (rewardItemUi == null)
            return;

        bool hasAmount = showAmounts && reward.amount > BigDouble.Zero;
        rewardItemUi.SetIcon(icon);
        rewardItemUi.SetAmount(hasAmount ? reward.amount.ToString() : string.Empty, hasAmount);
    }

    private static void ApplyItemRewardVisualState(
        CachaResultItemUI rewardItemUi,
        RewardManager.DungeonRewardEntry reward,
        Sprite icon,
        bool showAmounts)
    {
        rewardItemUi.PrepareForCustomDisplay();
        rewardItemUi.SetIcon(icon);

        bool hasAmount = showAmounts && reward.amount > BigDouble.Zero;
        rewardItemUi.SetCountDisplay(hasAmount ? reward.amount.ToString() : string.Empty, hasAmount);
        rewardItemUi.SetGemDisplay(IsSkillScrollReward(reward.itemId));

        if (reward.visualType != RewardManager.DungeonRewardVisualType.Equipment)
        {
            rewardItemUi.ResetFrameTint();
            rewardItemUi.SetTierDisplay(false, 0, Color.white);
            return;
        }

        if (!TryResolveEquipmentInfo(reward, out EquipListTable equipInfo))
        {
            rewardItemUi.ResetFrameTint();
            rewardItemUi.SetTierDisplay(false, 0, Color.white);
            return;
        }

        bool isActualEquipmentReward = reward.itemId > 0;
        rewardItemUi.SetTierDisplay(
            isActualEquipmentReward,
            equipInfo.grade,
            RarityColor.TierColorByTier(equipInfo.grade));

        if (isActualEquipmentReward)
        {
            rewardItemUi.SetCountDisplay(string.Empty, false);
            rewardItemUi.SetFrameTint(RarityColor.ItemGradeColor(equipInfo.rarityType));
        }
        else
        {
            rewardItemUi.ResetFrameTint();
        }
    }

    private static void ApplyEquipmentRewardVisualState(
        EquipItemUI equipmentItemUi,
        RewardManager.DungeonRewardEntry reward,
        Sprite icon)
    {
        equipmentItemUi.EnsureBindings();

        if (equipmentItemUi.Button != null)
        {
            equipmentItemUi.Button.onClick.RemoveAllListeners();
            equipmentItemUi.Button.interactable = false;
        }

        if (equipmentItemUi.MergeSlider != null)
            equipmentItemUi.MergeSlider.gameObject.SetActive(false);

        EquipItemView equipmentView = new EquipItemView(equipmentItemUi);
        if (TryResolveEquipmentInfo(reward, out EquipListTable equipInfo))
        {
            equipmentView.Render(
                icon,
                string.Empty,
                GetEquipmentStarCount(equipInfo.grade),
                RarityColor.TierColorByTier(equipInfo.grade));
            equipmentView.SetFrameColor(RarityColor.ItemGradeColor(equipInfo.rarityType));
        }
        else
        {
            equipmentView.Render(icon, string.Empty, 1, Color.white);
            equipmentView.SetFrameColor(Color.white);
        }
    }

    private static TextMeshProUGUI CreateAmountText(Transform rewardItem, int layer)
    {
        GameObject textObject = new GameObject(
            "(Text)RewardAmount",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI),
            typeof(Outline));
        textObject.layer = layer;
        textObject.transform.SetParent(rewardItem, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.08f, 0.02f);
        textRect.anchorMax = new Vector2(0.92f, 0.32f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI amountText = textObject.GetComponent<TextMeshProUGUI>();
        amountText.raycastTarget = false;
        amountText.fontSize = 20f;
        amountText.enableAutoSizing = true;
        amountText.fontSizeMin = 10f;
        amountText.fontSizeMax = 20f;
        amountText.color = Color.white;
        amountText.alignment = TextAlignmentOptions.BottomRight;

        Outline outline = textObject.GetComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        return amountText;
    }

    private static bool TryResolveEquipmentInfo(RewardManager.DungeonRewardEntry reward, out EquipListTable equipInfo)
    {
        equipInfo = null;

        if (DataManager.Instance?.EquipListDict == null)
            return false;

        if (reward.itemId > 0 && DataManager.Instance.EquipListDict.TryGetValue(reward.itemId, out equipInfo))
            return equipInfo != null;

        foreach (EquipListTable candidate in DataManager.Instance.EquipListDict.Values)
        {
            if (candidate == null)
                continue;

            if (candidate.equipmentType == reward.equipmentType && candidate.equipmentTier == reward.equipmentTier)
            {
                equipInfo = candidate;
                return true;
            }
        }

        foreach (EquipListTable candidate in DataManager.Instance.EquipListDict.Values)
        {
            if (candidate == null)
                continue;

            if (candidate.equipmentTier == reward.equipmentTier)
            {
                equipInfo = candidate;
                return true;
            }
        }

        return false;
    }

    private static bool IsSkillScrollReward(int itemId)
    {
        if (itemId <= 0 || DataManager.Instance?.ItemInfoDict == null)
            return false;

        if (!DataManager.Instance.ItemInfoDict.TryGetValue(itemId, out ItemInfoTable itemInfo) || itemInfo == null)
            return false;

        return itemInfo.itemType == ItemType.SkillScroll;
    }

    private static int GetEquipmentStarCount(int grade)
    {
        return ((Mathf.Max(1, grade) - 1) % 5) + 1;
    }

    private static void RefreshRewardLayout(RectTransform rewardRoot)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(rewardRoot);
        Canvas.ForceUpdateCanvases();

        ScrollRect scrollRect = rewardRoot.GetComponentInParent<ScrollRect>(true);
        if (scrollRect == null || scrollRect.content != rewardRoot)
            return;

        if (scrollRect.horizontal)
            scrollRect.horizontalNormalizedPosition = 0f;

        if (scrollRect.vertical)
            scrollRect.verticalNormalizedPosition = 1f;
    }
}

internal sealed class DungeonRewardItemUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI amountText;

    public void Bind(Image icon, TextMeshProUGUI amount)
    {
        iconImage = icon;
        amountText = amount;
    }

    public void SetIcon(Sprite icon)
    {
        if (iconImage == null)
            return;

        iconImage.sprite = icon;
        iconImage.enabled = icon != null;
        iconImage.preserveAspect = true;
    }

    public void SetAmount(string amount, bool visible)
    {
        if (amountText == null)
            return;

        amountText.gameObject.SetActive(visible);
        if (visible)
            amountText.text = amount;
    }
}
