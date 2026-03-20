using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 프리셋용 스킬 슬롯 카드 하나를 구성하는 레거시 아이템 뷰입니다.
/// </summary>
public class SkillSlotItem : MonoBehaviour
{
    [Header("스킬 정보")]
    // 스킬 대표 아이콘입니다.
    [SerializeField] private Image skillIcon;
    // 스킬 이름 텍스트입니다.
    [SerializeField] private TMP_Text skillNameText;
    // 스킬 레벨 텍스트입니다.
    [SerializeField] private TMP_Text levelText;
    // 등급 및 조각 수 텍스트입니다.
    [SerializeField] private TMP_Text gradeText;

    [Header("젬 슬롯")]
    // M5 젬 슬롯 아이콘 배열입니다.
    [SerializeField] private Image[] m5JemIcons;
    // M4 젬 슬롯 아이콘입니다.
    [SerializeField] private Image m4JemIcon;

    [Header("버튼")]
    // 장착 요청 버튼입니다.
    [SerializeField] private Button equipButton;

    // 현재 슬롯이 표시 중인 스킬 ID입니다.
    private int skillID = -1;
    // 장착 버튼 클릭 시 전달할 콜백입니다.
    private Action<int> onEquipClicked;

    // 버튼 클릭 이벤트를 한 번만 연결합니다.
    private void Awake()
    {
        equipButton.onClick.AddListener(OnEquipButtonClicked);
    }

    // 오브젝트 파괴 시 버튼 리스너를 정리합니다.
    private void OnDestroy()
    {
        equipButton.onClick.RemoveListener(OnEquipButtonClicked);
    }

    // 스킬 ID와 장착 콜백을 바인딩한 뒤 화면을 갱신합니다.
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

    // 현재 스킬 ID 기준으로 카드 정보를 다시 그립니다.
    public void Rebuild()
    {
        if (DataManager.Instance == null || DataManager.Instance.SkillInfoDict == null)
            return;

        if (!DataManager.Instance.SkillInfoDict.TryGetValue(skillID, out var table))
            return;

        skillNameText.SetText(table.skillName);
        skillIcon.sprite = SkillIconResolver.TryLoad(table.skillIcon, skillID);

        var skillModule = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetModule<SkillInventoryModule>()
            : null;
        var owned = skillModule != null ? skillModule.GetSkillData(skillID) : null;

        if (owned != null)
            SetOwned(owned);
        else
            SetLocked();
    }

    // 소유 중인 스킬 카드 상태를 화면에 반영합니다.
    private void SetOwned(OwnedSkillData data)
    {
        gradeText.SetText($"{GetGradeDisplayText(data.GetGrade())} : {data.GetOwnedScrollCount()} / {data.GetLevelUpCost()}");

        levelText.SetText("Lv.{0}", data.level);

        skillIcon.color = skillIcon.sprite != null ? Color.white : Color.gray;
        equipButton.interactable = data.IsEquippable;
        equipButton.image.color = data.IsEquippable ? Color.white : Color.gray;

        for (int i = 0; i < m5JemIcons.Length; i++)
        {
            m5JemIcons[i].gameObject.SetActive(true);
            m5JemIcons[i].color = data.IsM5JemSlotOpen(i) ? Color.white : Color.gray;
        }

        m4JemIcon.gameObject.SetActive(true);
        m4JemIcon.color = data.IsM4JemSlotOpen ? Color.white : Color.gray;
    }

    // 아직 잠겨 있는 스킬 카드 상태를 화면에 반영합니다.
    private void SetLocked()
    {
        gradeText.SetText("None");
        levelText.SetText(string.Empty);
        skillIcon.color = skillIcon.sprite != null ? new Color(1f, 1f, 1f, 0.35f) : Color.gray;
        equipButton.interactable = false;
        equipButton.image.color = Color.gray;

        for (int i = 0; i < m5JemIcons.Length; i++)
            m5JemIcons[i].gameObject.SetActive(false);

        m4JemIcon.gameObject.SetActive(false);
    }

    // 장착 버튼 클릭을 외부 콜백으로 전달합니다.
    private void OnEquipButtonClicked()
    {
        onEquipClicked?.Invoke(skillID);
    }

    // 스킬 등급 enum을 UI 문자열로 변환합니다.
    private string GetGradeDisplayText(SkillGrade grade)
    {
        switch (grade)
        {
            case SkillGrade.Scroll:
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
