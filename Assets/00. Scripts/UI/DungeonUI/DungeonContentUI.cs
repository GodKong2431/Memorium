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

    [Header("State")]
    [SerializeField] private GameObject contentLockPanel;
    [SerializeField] private Button enterButton;
    [SerializeField] private TextMeshProUGUI neededKeyText;

    public void SetEnterInteractable(bool interactable) { enterButton.interactable = interactable; }
    public void SetDungeonName(string dungeonName) { dungeonNameText.text = dungeonName; }
    public void SetLocked(bool isLocked) { contentLockPanel.SetActive(isLocked); }

    public void SetNeededKeyState(BigDouble currentKey, int requiredKey, Color normalColor, Color notEnoughColor)
    {
        neededKeyText.text = requiredKey.ToString();
        neededKeyText.color = currentKey >= new BigDouble(requiredKey) ? normalColor : notEnoughColor;
    }

    public void BindEnter(UnityAction onClick)
    {
        enterButton.onClick.RemoveAllListeners();
        enterButton.onClick.AddListener(onClick);
    }

    public void RebuildRewards(IReadOnlyList<int> rewardItemIds, Func<int, Sprite> iconResolver)
    {
        Transform template = rewardContentRoot.GetChild(0);
        for (int i = rewardContentRoot.childCount - 1; i > 0; i--)
            Destroy(rewardContentRoot.GetChild(i).gameObject);

        if (rewardItemIds == null || rewardItemIds.Count == 0)
        {
            template.gameObject.SetActive(false);
            return;
        }

        for (int i = 0; i < rewardItemIds.Count; i++)
        {
            Transform rewardItem = i == 0 ? template : Instantiate(template, rewardContentRoot, false);
            rewardItem.name = $"(Btn)ItemFrame_{rewardItemIds[i]}";
            rewardItem.gameObject.SetActive(true);

            Sprite icon = iconResolver != null ? iconResolver(rewardItemIds[i]) : null;
            ApplyRewardIcon(rewardItem, icon);
        }
    }

    private static void ApplyRewardIcon(Transform rewardItem, Sprite icon)
    {
        Image[] images = rewardItem.GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].transform == rewardItem)
                continue;

            images[i].sprite = icon;
            return;
        }
    }
}
