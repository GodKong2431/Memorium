using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EquipSlotView
{
    private readonly GameObject slot;

    public EquipSlotView(GameObject slot)
    {
        this.slot = slot;
    }

    public void Render(EquipListTable info)
    {
        // Toggle 슬롯은 원색 유지, 일반 슬롯은 희귀도 색으로 표시한다.
        Image image = slot.GetComponent<Image>();
        if (image != null)
        {
            if (slot.GetComponent<Toggle>() != null)
                image.color = Color.white;
            else
                image.color = RarityColor.ItemGradeColor(info.rarityType);
        }

        string label = info.description + "\n" + info.equipmentName;

        // 기존 Text/TMP_Text 중 씬에 있는 타입에 맞춰 텍스트를 갱신한다.
        Text legacyText = slot.GetComponentInChildren<Text>(true);
        if (legacyText != null)
            legacyText.text = label;

        TMP_Text tmpText = slot.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = label;
    }
}

