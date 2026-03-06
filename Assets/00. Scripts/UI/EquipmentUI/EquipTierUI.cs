using UnityEngine;
using UnityEngine.UI;

public class EquipTierUI : MonoBehaviour
{
    // 인스펙터에서 연결하는 티어 그룹 바인딩 컴포넌트.
    [SerializeField] private Image tierPanel;

    [SerializeField] private RectTransform tierRoot;

    [SerializeField] private RectTransform listRoot;

    public Image TierPanel => tierPanel;
    public RectTransform TierRoot => tierRoot;
    public RectTransform ListRoot => listRoot;
}

