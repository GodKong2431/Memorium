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
        public PlayerStatType type;
        public StatUpgradeUIItem statUIItem;
    }

    [System.Serializable]
    public struct FinalStatUI
    {
        public PlayerStatType type;
        public string statName;
        public FinalStatUIItem statUIItem;
    }

    public struct TraitUI
    {
        public PlayerStatType type;

    }

    public List<StatUpgradUI> upgradeStatUIs;

    public List<FinalStatUI> finalStatUIs;

    [SerializeField] TextMeshProUGUI normalPowerText;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => CharacterStatManager.Instance != null);
        yield return new WaitUntil(() => CharacterStatManager.Instance.TableLoad);


        foreach (var finalStatUI in finalStatUIs)
        {
            BigDouble value = CharacterStatManager.Instance.GetFinalStat(finalStatUI.type);
            finalStatUI.statUIItem.FinalStatValue.text = value.ToString();
            finalStatUI.statUIItem.FinalStatName.text = finalStatUI.statName;
        }

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

                upgradeStatUI.statUIItem.StatValue.text = value.ToString();
                upgradeStatUI.statUIItem.StatDescription.text = $"{statUpgrade.StatName} Enchance";
                upgradeStatUI.statUIItem.StatLevel.text = $"Lv {statUpgrade.UpgradeCount.ToString()}";
                upgradeStatUI.statUIItem.UpgradeCost.text = $"{statUpgrade.CurrentCost.ToString()}";
                upgradeStatUI.statUIItem.UpgradeBtn.onClick.AddListener(() => statUpgrade.Upgrade());

                statUpgrade.UpgradeStat += SetStat;
            }
        }
    }

    public void SetFinalStat()
    {
        foreach (var finalStatUI in finalStatUIs)
        {
            BigDouble value = CharacterStatManager.Instance.GetFinalStat(finalStatUI.type);
            finalStatUI.statUIItem.FinalStatValue.text = value.ToString();
            finalStatUI.statUIItem.FinalStatName.text = finalStatUI.statName;
        }
    }

    public void SetStat(PlayerStatType statType)
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
                Debug.Log("스탯 호출");
                BigDouble value = statUpgrade.Stat;

                statui.statUIItem.StatValue.text = value.ToString();
                statui.statUIItem.StatDescription.text = $"{statUpgrade.StatName} Enchance";
                statui.statUIItem.StatLevel.text = $"Lv {statUpgrade.UpgradeCount.ToString()}";
                statui.statUIItem.UpgradeCost.text = $"{statUpgrade.CurrentCost.ToString()}";
            }
        }
    }

    public void CheakGold(CurrencyType currencyType, BigDouble value)
    {
        Debug.Log("골드 체크 호출");
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
        BigDouble value = CharacterStatManager.Instance.NormalPower;
        normalPowerText.text = value.ToString();
    }
}
