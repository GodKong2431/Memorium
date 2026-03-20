using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class StoneUI
{
    private void RenderStoneItem(StoneItemUI itemUI, StoneGrade grade, AbilityStone stoneData, int tier)
    {
        if (itemUI == null)
        {
            return;
        }

        bool unlocked = IsStoneUnlocked(grade, tier);
        bool isSelected = selectedGrade == grade
            && selectedtier == tier
            && upgradePanel != null
            && upgradePanel.gameObject.activeSelf;

        if (itemUI.FrameImage != null)
        {
            itemUI.FrameImage.color = isSelected
                ? selectedStoneColor
                : unlocked
                    ? unlockedStoneColor
                    : lockedStoneColor;
        }

        if (itemUI.GradeText != null)
        {
            itemUI.GradeText.text = $"{tier}티어 {GetGradeName(grade)} {TextStone}";
            itemUI.GradeText.color = unlocked ? AbilityStoneManager.Instance.so.StoneGradeColorDict[grade] : disabledTextColor;
        }

        itemUI.SetLocked(!unlocked);

        TextMeshProUGUI[] slotTexts = itemUI.SlotTexts;
        for (int i = 0; i < slotTexts.Length; i++)
        {
            AbilityStoneSlot slotData = i < stoneData.Slots.Count ? stoneData.Slots[i] : null;
            ApplyStatIcon(itemUI.GetSlotImage(i), slotData != null ? slotData.SlotType : StatType.None, unlocked);

            if (slotTexts[i] == null)
            {
                continue;
            }

            slotTexts[i].text = BuildStoneSlotText(slotData, i);
            slotTexts[i].color = !unlocked
                ? disabledTextColor
                : i == 2
                    ? penaltyResultColor
                    : Color.white;
        }
    }

    private void RenderBonusItem(StoneBonusItemUI itemUI, StoneTotalUpBonusA bonusData, int totalSuccessCount)
    {
        if (itemUI == null || bonusData == null)
        {
            return;
        }

        bool unlocked = bonusData.isUnlock;

        if (itemUI.BackgroundImage != null)
        {
            itemUI.BackgroundImage.color = unlocked ? unlockedBonusColor : lockedBonusColor;
        }

        if (itemUI.RequirementText != null)
        {
            itemUI.RequirementText.text = unlocked
                ? $"{bonusData.totalUpCount}회 달성"
                : $"{Mathf.Min(totalSuccessCount, bonusData.totalUpCount)} / {bonusData.totalUpCount}";
            itemUI.RequirementText.color = unlocked ? Color.white : disabledTextColor;
        }

        if (itemUI.ValueText != null)
        {
            itemUI.ValueText.text = FormatSignedStatValue(bonusData.statType, bonusData.increaseStat, true);
            itemUI.ValueText.color = unlocked ? Color.white : disabledTextColor;
        }

        Image[] icons = itemUI.StatIcons;
        for (int i = 0; i < icons.Length; i++)
        {
            ApplyStatIcon(icons[i], bonusData.statType, unlocked);
        }
    }

    private void RenderUpgradeSlotItem(StoneSlotItemUI itemUI, AbilityStone stoneData, int slotIndex, bool stoneUnlocked, bool canAffordUpgrade)
    {
        if (itemUI == null)
        {
            return;
        }

        AbilityStoneSlot slotData = slotIndex < stoneData.Slots.Count ? stoneData.Slots[slotIndex] : null;
        bool canAttempt = stoneUnlocked && stoneData.IsConfigured && stoneData.CanAttemptUpgrade(slotIndex) && canAffordUpgrade;

        ApplyStatIcon(itemUI.StatIconImage, slotData != null ? slotData.SlotType : StatType.None, stoneUnlocked && stoneData.IsConfigured);

        if (itemUI.StatNameText != null)
        {
            itemUI.StatNameText.text = slotData == null || slotData.SlotType == StatType.None
                ? TextNotConfigured
                : GetStatName(slotData.SlotType);
            itemUI.StatNameText.color = stoneUnlocked ? Color.white : disabledTextColor;
        }

        if (itemUI.SuccessCountText != null)
        {
            itemUI.SuccessCountText.text = $"{TextCurrent} {stoneData.GetSuccessCount(slotIndex)} / {stoneData.GetOpportunityCount(slotIndex)}";
            itemUI.SuccessCountText.color = stoneUnlocked ? Color.white : disabledTextColor;
        }

        if (itemUI.ButtonText != null)
        {
            itemUI.ButtonText.text = canAttempt ? TextUpgrade : BuildSlotStateText(stoneData, slotIndex, stoneUnlocked, canAffordUpgrade);
            itemUI.ButtonText.color = canAttempt ? Color.white : disabledTextColor;
        }

        if (itemUI.CostText != null)
        {
            itemUI.CostText.text = canAttempt ? FormatCurrency(stoneData.UpCost) : string.Empty;
            itemUI.CostText.color = canAttempt ? Color.white : disabledTextColor;
        }

        if (itemUI.Button != null)
        {
            itemUI.Button.interactable = canAttempt;
        }

        RefreshProgressIcons(itemUI.ProgressIcons, slotData, slotIndex, stoneData.GetOpportunityCount(slotIndex));
    }

    private void RefreshProgressIcons(IReadOnlyList<Image> icons, AbilityStoneSlot slotData, int slotIndex, int opportunityCount)
    {
        if (icons == null)
        {
            return;
        }

        int attemptCount = slotData != null ? slotData.successCounter.Count : 0;
        for (int i = 0; i < icons.Count; i++)
        {
            Image icon = icons[i];
            if (icon == null)
            {
                continue;
            }

            if (i >= opportunityCount)
            {
                icon.color = new Color(waitingResultColor.r, waitingResultColor.g, waitingResultColor.b, 0.15f);
                continue;
            }

            if (i >= attemptCount)
            {
                icon.color = waitingResultColor;
                continue;
            }

            bool success = slotData != null && slotData.successCounter[i];
            if (success)
            {
                icon.color = slotIndex == 2 ? penaltyResultColor : positiveResultColor;
            }
            else
            {
                icon.color = failResultColor;
            }
        }
    }

    private void RefreshLoadingState()
    {
        // 데이터가 아직 없을 때도 빈 화면 대신 로딩 상태를 보여준다.
        RefreshSharedInfo();
        RefreshStoneListLoading();
        RefreshBonusInfoLoading();
        RefreshUpgradePanelLoading();
    }

    private void RefreshStoneListLoading()
    {
        for (int i = 0; i < runtimeStoneItems.Count; i++)
        {
            StoneItemUI itemUI = runtimeStoneItems[i];
            if (itemUI == null)
            {
                continue;
            }

            bool isSelected = selectedGrade == (StoneGrade)i && upgradePanel != null && upgradePanel.gameObject.activeSelf;

            if (itemUI.FrameImage != null)
            {
                itemUI.FrameImage.color = isSelected ? selectedStoneColor : lockedStoneColor;
            }

            if (itemUI.GradeText != null)
            {
                itemUI.GradeText.text = $"{GetGradeName((StoneGrade)i)} {TextStone}";
                itemUI.GradeText.color = disabledTextColor;
            }

            itemUI.SetLocked(true);

            TextMeshProUGUI[] slotTexts = itemUI.SlotTexts;
            for (int slotIndex = 0; slotIndex < slotTexts.Length; slotIndex++)
            {
                ApplyStatIcon(itemUI.GetSlotImage(slotIndex), StatType.None, false);

                if (slotTexts[slotIndex] != null)
                {
                    slotTexts[slotIndex].text = TextDataLoading;
                    slotTexts[slotIndex].color = disabledTextColor;
                }
            }
        }
    }

    private void RefreshBonusInfoLoading()
    {
        if (infoPanel == null)
        {
            return;
        }

        if (infoPanel.TitleText != null)
        {
            infoPanel.TitleText.text = TextBonusStat;
        }

        if (infoPanel.SummaryText != null)
        {
            infoPanel.SummaryText.text = TextDataLoading;
        }

        if (infoPanel.CurrentUpgradeValueText != null)
        {
            infoPanel.CurrentUpgradeValueText.text = "+0";
        }

        for (int i = 0; i < runtimeBonusItems.Count; i++)
        {
            StoneBonusItemUI itemUI = runtimeBonusItems[i];
            if (itemUI == null)
            {
                continue;
            }

            if (itemUI.BackgroundImage != null)
            {
                itemUI.BackgroundImage.color = lockedBonusColor;
            }

            if (itemUI.RequirementText != null)
            {
                itemUI.RequirementText.text = TextDataLoading;
                itemUI.RequirementText.color = disabledTextColor;
            }

            if (itemUI.ValueText != null)
            {
                itemUI.ValueText.text = string.Empty;
            }

            Image[] icons = itemUI.StatIcons;
            for (int iconIndex = 0; iconIndex < icons.Length; iconIndex++)
            {
                ApplyStatIcon(icons[iconIndex], StatType.None, false);
            }
        }
    }

    private void RefreshUpgradePanelLoading()
    {
        if (upgradePanel == null || selectedGrade == null)
        {
            return;
        }

        if (upgradePanel.GradeText != null)
        {
            upgradePanel.GradeText.text = $"{selectedtier}티어 {GetGradeName(selectedGrade.Value)} {TextStone}";
            upgradePanel.GradeText.color = AbilityStoneManager.Instance.so.StoneGradeColorDict[selectedGrade.Value];
        }

        TextMeshProUGUI[] probabilityTexts = upgradePanel.ProbabilityTexts;
        for (int i = 0; i < probabilityTexts.Length; i++)
        {
            if (probabilityTexts[i] != null)
            {
                probabilityTexts[i].text = TextDataLoading;
                probabilityTexts[i].color = disabledTextColor;
            }
        }

        StoneSlotItemUI[] slotItems = upgradePanel.SlotItems;
        for (int i = 0; i < slotItems.Length; i++)
        {
            StoneSlotItemUI itemUI = slotItems[i];
            if (itemUI == null)
            {
                continue;
            }

            ApplyStatIcon(itemUI.StatIconImage, StatType.None, false);

            if (itemUI.StatNameText != null)
            {
                itemUI.StatNameText.text = TextDataLoading;
                itemUI.StatNameText.color = disabledTextColor;
            }

            if (itemUI.SuccessCountText != null)
            {
                itemUI.SuccessCountText.text = $"{TextCurrent} 0 / 0";
                itemUI.SuccessCountText.color = disabledTextColor;
            }

            if (itemUI.ButtonText != null)
            {
                itemUI.ButtonText.text = TextDataLoading;
                itemUI.ButtonText.color = disabledTextColor;
            }

            if (itemUI.CostText != null)
            {
                itemUI.CostText.text = string.Empty;
                itemUI.CostText.color = disabledTextColor;
            }

            if (itemUI.Button != null)
            {
                itemUI.Button.interactable = false;
            }

            RefreshProgressIcons(itemUI.ProgressIcons, null, i, 0);
        }

        if (upgradePanel.NextGradeButton != null)
        {
            upgradePanel.NextGradeButton.interactable = false;
        }

        if (upgradePanel.NextGradeButtonText != null)
        {
            upgradePanel.NextGradeButtonText.text = TextDataLoading;
            upgradePanel.NextGradeButtonText.color = disabledTextColor;
        }

        if (upgradePanel.RerollButton != null)
        {
            upgradePanel.RerollButton.interactable = false;
        }

        if (upgradePanel.ResetButton != null)
        {
            upgradePanel.ResetButton.interactable = false;
        }

        if (upgradePanel.RerollButtonText != null)
        {
            upgradePanel.RerollButtonText.text = $"{TextReconfigure}\n{TextDataLoading}";
            upgradePanel.RerollButtonText.color = disabledTextColor;
        }

        if (upgradePanel.ResetButtonText != null)
        {
            upgradePanel.ResetButtonText.text = $"{TextReset}\n{TextDataLoading}";
            upgradePanel.ResetButtonText.color = disabledTextColor;
        }

        upgradePanel.HidePopups();
    }

    private void ToggleBonusInfoPanel()
    {
        // 보너스 패널과 강화 패널은 동시에 열리지 않게 유지한다.
        if (!HasSceneRefs())
        {
            return;
        }

        BuildIfNeeded();

        bool shouldOpen = infoPanel == null || !infoPanel.gameObject.activeSelf;
        CloseUpgradePanel();
        SetPanelActive(infoPanel != null ? infoPanel.gameObject : null, shouldOpen);

        if (shouldOpen)
        {
            if (TryPrepareRuntimeData())
            {
                RefreshBonusInfoPanel();
            }
            else
            {
                RefreshBonusInfoLoading();
            }
        }
    }

    private void CloseBonusInfoPanel()
    {
        SetPanelActive(infoPanel != null ? infoPanel.gameObject : null, false);
    }

    private void OpenUpgradePanel(StoneGrade grade, int tierIndex)
    {
        // 같은 스톤을 다시 누르면 팝업을 닫는다.
        if (!HasSceneRefs() || upgradePanel == null)
        {
            return;
        }

        BuildIfNeeded();

        if (selectedGrade == grade && selectedtier == tierIndex && upgradePanel.gameObject.activeSelf)
        {
            if (TryPrepareRuntimeData())
            {
                RefreshUpgradePanel();
                RefreshStoneList();
            }
            return;
        }

        selectedGrade = grade;
        selectedtier = tierIndex;
        SetPanelActive(infoPanel != null ? infoPanel.gameObject : null, false);
        SetPanelActive(upgradePanel.gameObject, true);
        upgradePanel.HidePopups();

        if (TryPrepareRuntimeData())
        {
            RefreshUpgradePanel();
            RefreshStoneList();
        }
        else
        {
            RefreshUpgradePanelLoading();
            RefreshStoneListLoading();
        }
    }

    private void CloseUpgradePanel()
    {
        if (upgradePanel != null)
        {
            upgradePanel.HidePopups();
            SetPanelActive(upgradePanel.gameObject, false);
        }

        selectedGrade = null;

        if (TryPrepareRuntimeData())
        {
            RefreshStoneList();
        }
        else
        {
            RefreshStoneListLoading();
        }
    }

    private void CloseUpgradePopups()
    {
        // 확인 팝업 두 개는 같은 함수로 함께 닫는다.
        upgradePanel?.HidePopups();
    }

    private void OpenReconfigurePopup()
    {
        AbilityStone stoneData = GetSelectedStone();
        if (stoneData == null || selectedGrade == null || upgradePanel == null)
        {
            return;
        }

        if (!IsStoneUnlocked(selectedGrade.Value, selectedtier))
        {
            return;
        }

        upgradePanel.HidePopups();
        if (upgradePanel.ReconfigurePopupRoot != null)
        {
            upgradePanel.ReconfigurePopupRoot.SetActive(true);
        }

        RefreshReconfigurePopup(stoneData, true);
    }

    private void OpenResetPopup()
    {
        AbilityStone stoneData = GetSelectedStone();
        if (stoneData == null || selectedGrade == null || upgradePanel == null)
        {
            return;
        }

        int totalAttemptCount = stoneData.GetAttemptCount(0) + stoneData.GetAttemptCount(1) + stoneData.GetAttemptCount(2);
        if (!IsStoneUnlocked(selectedGrade.Value, selectedtier) || totalAttemptCount <= 0)
        {
            return;
        }

        upgradePanel.HidePopups();
        if (upgradePanel.ResetPopupRoot != null)
        {
            upgradePanel.ResetPopupRoot.SetActive(true);
        }

        RefreshResetPopup(stoneData, true, totalAttemptCount);
    }

    private void UpdateNextGradeButton(StoneGrade grade, bool unlocked, int tier)
    {
        if (upgradePanel == null || upgradePanel.NextGradeButton == null)
        {
            return;
        }

        Button nextButton = upgradePanel.NextGradeButton;
        TextMeshProUGUI nextText = upgradePanel.NextGradeButtonText;

        if (!unlocked)
        {
            nextButton.interactable = false;
            if (nextText != null)
            {
                nextText.text = GetUnlockReason(grade, tier);
                nextText.color = disabledTextColor;
            }

            return;
        }

        bool hasNextTier = TryGetNextTierKey(tier, out int nextTierKey);
        if (grade == StoneGrade.Myth && !hasNextTier)
        {
            nextButton.interactable = false;
            if (nextText != null)
            {
                nextText.text = TextFinalGrade;
                nextText.color = disabledTextColor;
            }

            return;
        }

        StoneGrade nextGrade;
        int targetTier;

        if (grade == StoneGrade.Myth)
        {
            nextGrade = StoneGrade.Normal;
            targetTier = nextTierKey;
        }
        else
        {
            nextGrade = (StoneGrade)((int)grade + 1);
            targetTier = tier;
        }

        bool nextUnlocked = IsStoneUnlocked(nextGrade, targetTier);
        nextButton.interactable = nextUnlocked;

        if (nextText != null)
        {
            nextText.text = nextUnlocked
                ? $"{GetGradeName(nextGrade)} {TextOpen}"
                : GetUnlockReason(nextGrade, targetTier);
            nextText.color = nextUnlocked ? Color.white : disabledTextColor;
        }
    }

    private void RefreshReconfigurePopup(AbilityStone stoneData, bool unlocked)
    {
        if (upgradePanel == null)
        {
            return;
        }

        if (upgradePanel.ReconfigureInfoText != null)
        {
            upgradePanel.ReconfigureInfoText.text = TextReconfigureInfo;
        }

        TextMeshProUGUI[] slotTexts = upgradePanel.ReconfigureSlotTexts;
        Image[] slotImages = upgradePanel.ReconfigureSlotImages;
        
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] != null)
            {
                AbilityStoneSlot slotData = i < stoneData.Slots.Count ? stoneData.Slots[i] : null;
                slotTexts[i].text = BuildPopupSlotText(slotData, i);
                slotImages[i].sprite = IconManager.GetIcon(slotData.SlotType);
            }
        }

        if (upgradePanel.ReconfigureConfirmCostText != null)
        {
            upgradePanel.ReconfigureConfirmCostText.text = FormatCurrency(stoneData.StatRerollCost);
        }

        if (upgradePanel.ReconfigureConfirmButton != null)
        {
            upgradePanel.ReconfigureConfirmButton.interactable = unlocked && CanAfford(stoneData.StatRerollCost);
        }
    }

    private void RefreshResetPopup(AbilityStone stoneData, bool unlocked, int totalAttemptCount)
    {
        if (upgradePanel == null)
        {
            return;
        }

        if (upgradePanel.ResetInfoText != null)
        {
            upgradePanel.ResetInfoText.text = TextResetInfo;
        }

        TextMeshProUGUI[] slotTexts = upgradePanel.ResetSlotTexts;
        Image[] slotImegs = upgradePanel.ResetSlotImages;
        
        for (int i = 0; i < slotTexts.Length; i++)
        {
            if (slotTexts[i] != null)
            {
                slotTexts[i].text = $"{stoneData.GetSuccessCount(i)} / {stoneData.GetOpportunityCount(i)}";
                slotImegs[i].sprite = IconManager.GetIcon(stoneData.GetStatType(i));
            }
        }

        if (upgradePanel.ResetConfirmCostText != null)
        {
            upgradePanel.ResetConfirmCostText.text = FormatCurrency(stoneData.UpResetCostValue);
        }

        if (upgradePanel.ResetConfirmButton != null)
        {
            upgradePanel.ResetConfirmButton.interactable =
                unlocked &&
                totalAttemptCount > 0 &&
                CanAfford(stoneData.UpResetCostValue);
        }
    }

    private void OnClickNextGrade()
    {
        if (selectedGrade == null)
        {
            return;
        }

        StoneGrade nextGrade;
        int targetTier = selectedtier;

        if (selectedGrade == StoneGrade.Myth)
        {
            if (!TryGetNextTierKey(selectedtier, out targetTier))
            {
                return;
            }

            nextGrade = StoneGrade.Normal;
        }
        else
        {
            nextGrade = (StoneGrade)((int)selectedGrade.Value + 1);
        }

        if (!IsStoneUnlocked(nextGrade, targetTier))
        {
            return;
        }

        OpenUpgradePanel(nextGrade, targetTier);
    }

    private void OnClickUpgradeSlot(int slotIndex)
    {
        AbilityStone stoneData = GetSelectedStone();
        if (stoneData == null || selectedGrade == null)
        {
            return;
        }

        if (!IsStoneUnlocked(selectedGrade.Value, selectedtier))
        {
            return;
        }

        if (!stoneData.IsConfigured || !stoneData.CanAttemptUpgrade(slotIndex))
        {
            return;
        }

        if (!TrySpendGold(stoneData.UpCost))
        {
            return;
        }

        stoneData.UpStone(slotIndex);
        RefreshAll();
    }

    private void OnClickRerollSelectedStone()
    {
        AbilityStone stoneData = GetSelectedStone();
        if (stoneData == null || selectedGrade == null)
        {
            return;
        }

        if (!IsStoneUnlocked(selectedGrade.Value, selectedtier) || !TrySpendGold(stoneData.StatRerollCost))
        {
            return;
        }

        stoneData.Reset(stoneData.StoneGrade);
        CloseUpgradePopups();
        RefreshAll();
    }

    private void OnClickResetSelectedStone()
    {
        AbilityStone stoneData = GetSelectedStone();
        if (stoneData == null || selectedGrade == null)
        {
            return;
        }

        if (!IsStoneUnlocked(selectedGrade.Value, selectedtier) || !TrySpendGold(stoneData.UpResetCostValue))
        {
            return;
        }

        stoneData.ResetUp();
        CloseUpgradePopups();
        RefreshAll();
    }

    private void OnCurrencyChanged(CurrencyType type, BigDouble amount)
    {
        if (type == CurrencyType.Gold)
        {
            RefreshView();
        }
    }

    private void OnStatUpdated()
    {
        RefreshView();
    }

    private AbilityStone GetSelectedStone()
    {
        if (selectedGrade == null)
        {
            return null;
        }

        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || abilityStoneManager.so == null)
        {
            return null;
        }

        if (!abilityStoneManager.so.AbilityStoneDict.TryGetValue(selectedtier, out var tierStoneDict))
        {
            return null;
        }

        return tierStoneDict.TryGetValue(selectedGrade.Value, out AbilityStone stoneData) ? stoneData : null;
    }

    private List<StoneTotalUpBonusA> GetOrderedBonusData()
    {
        // 보너스 정보는 해금 요구 횟수 순서대로 보여준다.
        List<StoneTotalUpBonusA> bonusDataList = new List<StoneTotalUpBonusA>();

        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || abilityStoneManager.so == null)
        {
            return bonusDataList;
        }

        foreach (StoneTotalUpBonusA bonusData in abilityStoneManager.so.StoneTotalUpBonusDict.Values)
        {
            if (bonusData != null)
            {
                bonusDataList.Add(bonusData);
            }
        }

        bonusDataList.Sort((left, right) =>
        {
            int requirementCompare = left.totalUpCount.CompareTo(right.totalUpCount);
            return requirementCompare != 0 ? requirementCompare : left.statType.CompareTo(right.statType);
        });

        return bonusDataList;
    }

    private int GetTotalSuccessCount()
    {
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || !abilityStoneManager.LoadStone || abilityStoneManager.so == null)
        {
            return 0;
        }

        int totalSuccessCount = 0;
        foreach (var index in abilityStoneManager.so.AbilityStoneDict.Values)
        {
            foreach (AbilityStone stoneData in index.Values)
            {
                if (stoneData != null)
                {
                    totalSuccessCount += stoneData.GetUpCount();
                }
            }
        }
        

        return totalSuccessCount;
    }

    private bool CanAfford(int goldCost)
    {
        CurrencyInventoryModule currentCurrencyModule = currencyModule;
        if (currentCurrencyModule == null && InventoryManager.Instance != null)
        {
            currentCurrencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
            currencyModule = currentCurrencyModule;
        }

        return currentCurrencyModule != null
            && currentCurrencyModule.HasEnough(CurrencyType.Gold, new BigDouble(goldCost));
    }

    private bool TrySpendGold(int goldCost)
    {
        CurrencyInventoryModule currentCurrencyModule = currencyModule;
        if (currentCurrencyModule == null && InventoryManager.Instance != null)
        {
            currentCurrencyModule = InventoryManager.Instance.GetModule<CurrencyInventoryModule>();
            currencyModule = currentCurrencyModule;
        }

        if (currentCurrencyModule == null)
        {
            return false;
        }

        if (currentCurrencyModule.TrySpend(CurrencyType.Gold, new BigDouble(goldCost)))
        {
            return true;
        }

        InstanceMessageManager.TryShowInsufficientGold();
        return false;
    }

    private void ApplyStatIcon(Image targetImage, StatType statType, bool isEnabled)
    {
        if (targetImage == null)
        {
            return;
        }

        Sprite icon = null;
        bool hasIcon = statType != StatType.None
            && iconByStat.TryGetValue(statType, out icon)
            && icon != null;

        targetImage.sprite = hasIcon ? icon : null;
        targetImage.enabled = hasIcon;

        if (hasIcon)
        {
            targetImage.color = isEnabled ? Color.white : disabledTextColor;
        }
    }

    private static string GetGradeName(StoneGrade grade)
    {
        return grade switch
        {
            StoneGrade.Normal => "일반",
            StoneGrade.Rare => "희귀",
            StoneGrade.Unique => "유니크",
            StoneGrade.Legendy => "전설",
            StoneGrade.Myth => "신화",
            _ => grade.ToString()
        };
    }

    private string GetUnlockReason(StoneGrade grade, int tier)
    {
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || !abilityStoneManager.LoadStone || abilityStoneManager.so == null)
        {
            return TextDataLoading;
        }

        if (!Enum.IsDefined(typeof(StoneGrade), grade))
        {
            return TextNoData;
        }

        if (!abilityStoneManager.so.AbilityStoneDict.TryGetValue(tier, out var tierStoneDict))
        {
            return TextNoData;
        }

        if (!tierStoneDict.TryGetValue(grade, out AbilityStone stoneData) || stoneData == null)
        {
            return TextNoData;
        }

        int currentLevel = stats != null && stats.LevelBonus != null
            ? stats.LevelBonus.CurrentLevel
            : 0;
        if (currentLevel < stoneData.UnlockLevel)
        {
            return $"Lv.{stoneData.UnlockLevel} {TextNeed}";
        }

        if (grade == StoneGrade.Normal)
        {
            return TextOpen;
        }

        StoneGrade previousGrade = (StoneGrade)((int)grade - 1);

        if (!tierStoneDict.TryGetValue(previousGrade, out AbilityStone previousStone) || previousStone == null)
        {
            return TextNoData;
        }

        int currentUpCount = previousStone.GetUpCount();
        if (currentUpCount < stoneData.NeedUp)
        {
            return $"{currentUpCount} / {stoneData.NeedUp}{TextTimesNeed}";
        }

        return TextOpen;
    }

    private bool IsStoneUnlocked(StoneGrade grade, int tier)
    {
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || !abilityStoneManager.LoadStone || abilityStoneManager.so == null)
        {
            return false;
        }

        if (!Enum.IsDefined(typeof(StoneGrade), grade))
        {
            return false;
        }

        if (!abilityStoneManager.so.AbilityStoneDict.TryGetValue(tier, out var tierStoneDict))
        {
            return false;
        }

        if (!tierStoneDict.TryGetValue(grade, out AbilityStone stoneData) || stoneData == null)
        {
            return false;
        }

        int currentLevel = stats != null && stats.LevelBonus != null
            ? stats.LevelBonus.CurrentLevel
            : 0;
        if (currentLevel < stoneData.UnlockLevel)
        {
            return false;
        }

        if (grade == StoneGrade.Normal)
        {
            stoneData.isUnlock = true;
            return true;
        }

        StoneGrade previousGrade = (StoneGrade)((int)grade - 1);

        if (!tierStoneDict.TryGetValue(previousGrade, out AbilityStone previousStone) || previousStone == null)
        {
            return false;
        }
        
        if (!stoneData.isUnlock)
        {
            stoneData.isUnlock = previousStone.GetUpCount() >= stoneData.NeedUp;
            return previousStone.GetUpCount() >= stoneData.NeedUp;
        }
        
        return true;
    }

    private bool TryGetNextTierKey(int currentTier, out int nextTier)
    {
        nextTier = currentTier;

        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager == null || abilityStoneManager.so == null)
        {
            return false;
        }

        int candidateTier = int.MaxValue;
        foreach (int tierKey in abilityStoneManager.so.AbilityStoneDict.Keys)
        {
            if (tierKey > currentTier && tierKey < candidateTier)
            {
                candidateTier = tierKey;
            }
        }

        if (candidateTier == int.MaxValue)
        {
            return false;
        }

        nextTier = candidateTier;
        return true;
    }

    private static string GetStatName(StatType statType)
    {
        if (statType == StatType.None)
        {
            return TextNotConfigured;
        }
        // 상위 스톤은 레벨과 이전 등급 강화 수를 같이 확인한다.
        AbilityStoneManager abilityStoneManager = AbilityStoneManager.Instance;
        if (abilityStoneManager != null && abilityStoneManager.LoadStone && abilityStoneManager.so != null)
        {
            if (abilityStoneManager.so.StoneGradeStatUpDict.TryGetValue(statType, out StoneGradeStatUp statUpData)
                && !string.IsNullOrWhiteSpace(statUpData.statName))
            {
                return statUpData.statName;
            }

            if (abilityStoneManager.so.StoneTotalUpBonusDict.TryGetValue(statType, out StoneTotalUpBonusA bonusData)
                && !string.IsNullOrWhiteSpace(bonusData.statName))
            {
                return bonusData.statName;
            }
        }

        return statType.ToString();
    }

    private static string BuildStoneSlotText(AbilityStoneSlot slotData, int slotIndex)
    {
        if (slotData == null || slotData.SlotType == StatType.None)
        {
            return TextNotConfigured;
        }

        float displayValue = slotIndex == 2 ? -slotData.totalStat : slotData.totalStat;
        return FormatSignedStatValue(slotData.SlotType, displayValue, false);
    }

    private static string BuildPopupSlotText(AbilityStoneSlot slotData, int slotIndex)
    {
        if (slotData == null || slotData.SlotType == StatType.None)
        {
            return TextNotConfigured;
        }

        float displayValue = slotIndex == 2 ? -slotData.increaseStat : slotData.increaseStat;
        return $"{GetStatName(slotData.SlotType)} {FormatSignedStatValue(slotData.SlotType, displayValue, false)}";
    }

    private static string BuildSlotStateText(AbilityStone stoneData, int slotIndex, bool stoneUnlocked, bool canAffordUpgrade)
    {
        if (!stoneUnlocked)
        {
            return TextLocked;
        }

        if (!stoneData.IsConfigured)
        {
            return TextNeedSetup;
        }

        if (!stoneData.CanAttemptUpgrade(slotIndex))
        {
            return TextUpgradeComplete;
        }

        if (!canAffordUpgrade)
        {
            return TextNotEnoughGold;
        }

        return TextUpgrade;
    }

    private static string FormatSignedStatValue(StatType statType, float value, bool mult)
    {
        string sign = value >= 0f ? "+" : "-";
        float absValue = Mathf.Abs(value);
        
        if (mult)
        {
            return $"{sign}{(absValue * 100f):0.###}%";
        }
        
        else if (StatGroups.MultTypes.Contains(statType))
        {
            return $"{sign}{(absValue * 100f):0.###}%";
        }

        return $"{sign}{absValue:0.###}";
    }

    private static string FormatCurrency(int value)
    {
        return new BigDouble(value).ToString();
    }

    private static void SetPanelActive(GameObject panelRoot, bool isActive)
    {
        if (panelRoot != null && panelRoot.activeSelf != isActive)
        {
            panelRoot.SetActive(isActive);
        }
    }
}
