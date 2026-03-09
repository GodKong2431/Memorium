using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatView : MonoBehaviour
{
    [System.Serializable]
    public struct StatUpgradUI
    {
        public StatType type;
        public StatUpgradeUIItem statUIItem;
    }

    [System.Serializable]
    public struct FinalStatUI
    {
        public StatType type;
        public string statName;
        public FinalStatUIItem statUIItem;
    }

    [System.Serializable]
    public struct GaugeUI
    {
        public Image fillImage;
        public TextMeshProUGUI valueText;
    }

    private PlayerStateContext playerContext;
    [SerializeField] private GaugeUI hpGauge;
    [SerializeField] private GaugeUI mpGauge;
    [SerializeField] private GaugeUI expGauge;

    public struct TraitUI
    {
        public StatType type;

    }

    public List<StatUpgradUI> upgradeStatUIs;

    public List<FinalStatUI> finalStatUIs;

    [SerializeField] TextMeshProUGUI normalPowerText;

    //private static readonly HashSet<PlayerStatType> MultTypes = new HashSet<PlayerStatType>
    //{
    //    PlayerStatType.CRIT_CHANCE,
    //    PlayerStatType.CRIT_MULT,
    //    PlayerStatType.NORMAL_DMG,
    //    PlayerStatType.BOSS_DMG,
    //    PlayerStatType.DMG_MULT,
    //    PlayerStatType.EXP_GAIN,
    //    PlayerStatType.GOLD_GAIN,
    //    PlayerStatType.COOLDOWN_REDUCE,
    //};

    IEnumerator Start()
    {
        yield return new WaitUntil(() => CharacterStatManager.Instance != null);
        yield return new WaitUntil(() => CharacterStatManager.Instance.TableLoad);


        //foreach (var finalStatUI in finalStatUIs)
        //{
        //    BigDouble value = CharacterStatManager.Instance.GetFinalStat(finalStatUI.type);
        //    finalStatUI.statUIItem.FinalStatValue.text = value.ToString();
        //    finalStatUI.statUIItem.FinalStatName.text = finalStatUI.statName;
        //}

        SetFinalStat();
        GetNormalPowerStat();

        CharacterStatManager.Instance.StatUpdate += SetFinalStat;
        CharacterStatManager.Instance.StatUpdate += GetNormalPowerStat;

        GameEventManager.OnCurrencyChanged += CheakGold;

        foreach (var upgradeStatUI in upgradeStatUIs)
        {
            var statUpgrade = CharacterStatManager.Instance.GetUpgradeTable(upgradeStatUI.type);

            if (upgradeStatUI.statUIItem != null)
            {
                BigDouble value = statUpgrade.Stat;

                if (StatGroups.MultTypes.Contains(upgradeStatUI.type))
                {
                    upgradeStatUI.statUIItem.StatValue.text = $"{value * 100f}%" ;
                }
                else
                {
                    upgradeStatUI.statUIItem.StatValue.text = value.ToString();
                }
                upgradeStatUI.statUIItem.StatDescription.text = $"{statUpgrade.StatName} Enchance";
                upgradeStatUI.statUIItem.StatLevel.text = $"Lv {statUpgrade.UpgradeCount.ToString()}";
                upgradeStatUI.statUIItem.UpgradeCost.text = $"{statUpgrade.CurrentCost.ToString()}";
                upgradeStatUI.statUIItem.UpgradeBtn.onClick.AddListener(() => statUpgrade.Upgrade());

                statUpgrade.UpgradeStat += SetStat;
            }
        }

        GameEventManager.OnCurrencyChanged += UpdateExpFromEvent;

        var currencyModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<CurrencyInventoryModule>()
            : null;
        if (currencyModule != null)
        {
            BigDouble currentExp = currencyModule.GetAmount(CurrencyType.Exp);
            UpdateExpUI(currentExp);
        }
    }

    public void InitContext(PlayerStateContext context)
    {
        this.playerContext = context;

        if (playerContext != null)
        {
            playerContext.OnHealthChanged += UpdateHealthUI;
            playerContext.OnManaChanged += UpdateManaUI;
            
            UpdateHealthUI(playerContext.CurrentHealth, playerContext.MaxHealth);
            UpdateManaUI(playerContext.CurrentMana, playerContext.MaxMana);
        }
    }

    public void SetFinalStat()
    {
        var statManager = CharacterStatManager.Instance;
        if (statManager == null || finalStatUIs == null)
            return;

        foreach (var finalStatUI in finalStatUIs)
        {
            if (finalStatUI.statUIItem == null)
                continue;

            BigDouble value = statManager.GetFinalStat(finalStatUI.type);

            if (StatGroups.MultTypes.Contains(finalStatUI.type))
            {
                if (finalStatUI.statUIItem.FinalStatValue != null)
                    finalStatUI.statUIItem.FinalStatValue.text = $"{value * 100f}%";
            }
            else
            {
                if (finalStatUI.statUIItem.FinalStatValue != null)
                    finalStatUI.statUIItem.FinalStatValue.text = value.ToString();
            }

            if (finalStatUI.statUIItem.FinalStatName != null)
                finalStatUI.statUIItem.FinalStatName.text = finalStatUI.statName;
        }
    }

    public void SetStat(StatType statType)
    {
        foreach (var statui in upgradeStatUIs)
        {
            if (statType != statui.type)
            {
                continue;
            }

            var statUpgrade = CharacterStatManager.Instance.GetUpgradeTable(statui.type);

            if (statui.statUIItem != null)
            {
                //Debug.Log("스탯 호출");
                BigDouble value = statUpgrade.Stat;

                if (StatGroups.MultTypes.Contains(statui.type))
                {
                    statui.statUIItem.StatValue.text = $"{value * 100f}%";
                }
                else
                {
                    statui.statUIItem.StatValue.text = value.ToString();
                }
                statui.statUIItem.StatDescription.text = $"{statUpgrade.StatName} Enchance";
                statui.statUIItem.StatLevel.text = $"Lv {statUpgrade.UpgradeCount.ToString()}";
                statui.statUIItem.UpgradeCost.text = $"{statUpgrade.CurrentCost.ToString()}";
            }
        }
    }

    public void CheakGold(CurrencyType currencyType, BigDouble value)
    {
        //Debug.Log("골드 체크 호출");
        if (currencyType != CurrencyType.Gold)
        {
            return;
        }
        StartCoroutine(NextGoldCheck());

    }

    IEnumerator NextGoldCheck()
    {
        yield return null;
        ApplyGold();

    }

    private void ApplyGold()
    {
        foreach (var statui in upgradeStatUIs)
        {
            var statUpgrade = CharacterStatManager.Instance.GetUpgradeTable(statui.type);

            if (statui.statUIItem != null)
            {
                statui.statUIItem.UpgradeCost.color = statUpgrade.CheckGold() ? Color.white : Color.red;
            }
        }
    }

    public void GetNormalPowerStat()
    {
        if (normalPowerText == null)
            return;

        var statManager = CharacterStatManager.Instance;
        if (statManager == null)
            return;

        BigDouble value = statManager.NormalPower;
        normalPowerText.text = value.ToString();
    }

    private void UpdateHealthUI(float current, float max)
    {
        if (hpGauge.fillImage != null)
            hpGauge.fillImage.fillAmount = max > 0 ? current / max : 0f;

        if (hpGauge.valueText != null)
            hpGauge.valueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void UpdateManaUI(float current, float max)
    {
        if (mpGauge.fillImage != null)
            mpGauge.fillImage.fillAmount = max > 0 ? current / max : 0f;

        if (mpGauge.valueText != null)
            mpGauge.valueText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
    private void UpdateExpFromEvent(CurrencyType type, BigDouble amount)
    {
        if (type == CurrencyType.Exp)
        {
            UpdateExpUI(amount);
        }
    }

    private void UpdateExpUI(BigDouble currentExp)
    {
        BigDouble requiredExp = CharacterStatManager.Instance.LevelBonus.RequiredExp;

        if (expGauge.fillImage != null)
        {
            if (requiredExp > 0)
            {
                float fillRatio = (currentExp / requiredExp).ToFloat();
                expGauge.fillImage.fillAmount = Mathf.Clamp01(fillRatio);
            }
            else
            {
                expGauge.fillImage.fillAmount = 0f;
            }
        }

        if (expGauge.valueText != null)
        {
            // "현재경험치 / 필요경험치" 형태로 표시
            expGauge.valueText.text = $"{currentExp.ToString()} / {requiredExp.ToString()}";
        }
    }
    private void OnDestroy()
    {
        if (CharacterStatManager.Instance != null)
        {
            CharacterStatManager.Instance.StatUpdate -= SetFinalStat;
            CharacterStatManager.Instance.StatUpdate -= GetNormalPowerStat;
        }

        GameEventManager.OnCurrencyChanged -= CheakGold;

        if (playerContext != null)
        {
            playerContext.OnHealthChanged -= UpdateHealthUI;
            playerContext.OnManaChanged -= UpdateManaUI;
            GameEventManager.OnCurrencyChanged -= UpdateExpFromEvent;
        }
    }
}
