using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillSlotItem : MonoBehaviour
{
    [Header("스킬 정보")]
    [SerializeField] private Image skillIcon;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text gradeText;

    [Header("젬 슬롯")]
    [SerializeField] private Image[] m5JemIcons;
    [SerializeField] private Image m4JemIcon;

    [Header("버튼")]
    [SerializeField] private Button equipButton;

    private int skillID = -1;
    private Action<int> onEquipClicked;

    private void Awake()
    {
        equipButton.onClick.AddListener(OnEquipButtonClicked);
    }

    private void OnDestroy()
    {
        equipButton.onClick.RemoveListener(OnEquipButtonClicked);
    }

    public void Init(int skillID, Action<int> onEquip)
    {
        if (this.skillID == skillID && onEquipClicked == onEquip)
        {
            Rebuild();
            return;
        }

        this.skillID = skillID;
        onEquipClicked = onEquip;
        Rebuild();
    }

    public void Rebuild()
    {
        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return;

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillID, out var table))
            return;

        skillNameText.SetText(table.skillName);

        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        var owned = skillModule != null ? skillModule.GetSkillData(skillID) : null;

        if (owned != null)
            SetOwned(owned);
        else
            SetLocked();
    }

    private void SetOwned(OwnedSkillData data)
    {
        gradeText.SetText($"{GetGradeDisplayText(data.HighestGrade)} : {data.HighestGradeCount}/3");

        if (data.HighestGrade >= SkillGrade.Rare)
            levelText.SetText("Lv.{0}", data.level);
        else
            levelText.SetText(string.Empty);

        skillIcon.color = Color.white;
        equipButton.interactable = data.IsEquippable;
        equipButton.image.color = data.IsEquippable ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);

        for (int i = 0; i < m5JemIcons.Length; i++)
        {
            m5JemIcons[i].gameObject.SetActive(true);
            m5JemIcons[i].color = data.IsM5JemSlotOpen(i) ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }

        m4JemIcon.gameObject.SetActive(true);
        m4JemIcon.color = data.IsM4JemSlotOpen ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.5f);
    }

    private void SetLocked()
    {
        gradeText.SetText("None");
        levelText.SetText(string.Empty);
        skillIcon.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        equipButton.interactable = false;
        equipButton.image.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        for (int i = 0; i < m5JemIcons.Length; i++)
            m5JemIcons[i].gameObject.SetActive(false);

        m4JemIcon.gameObject.SetActive(false);
    }

    private void OnEquipButtonClicked()
    {
        onEquipClicked?.Invoke(skillID);
    }

    private string GetGradeDisplayText(SkillGrade grade)
    {
        switch (grade)
        {
            case SkillGrade.Fragment:
                return "Scroll";
            case SkillGrade.Common:
                return "Common";
            case SkillGrade.Rare:
                return "Rare";
            case SkillGrade.Epic:
                return "Epic";
            case SkillGrade.Legendary:
                return "Legendary";
            case SkillGrade.Mythic:
                return "Mythic";
            default:
                return grade.ToString();
        }
    }
}
