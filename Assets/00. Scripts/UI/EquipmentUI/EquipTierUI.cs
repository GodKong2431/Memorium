using UnityEngine;
using UnityEngine.UI;

// 장비 티어 그룹 UI 오브젝트의 인스펙터 바인딩 홀더다.
public class EquipTierUI : MonoBehaviour
{
    // 티어 헤더 패널 이미지다.
    [SerializeField] private Image tierPanel;
    // 티어 헤더 별 아이콘 부모 트랜스폼이다.
    [SerializeField] private RectTransform tierRoot;
    // 해당 티어의 아이템 셀 부모 트랜스폼이다.
    [SerializeField] private RectTransform listRoot;

    // 티어 패널 바인딩을 외부에 노출한다.
    public Image TierPanel => tierPanel;
    // 티어 별 루트 바인딩을 외부에 노출한다.
    public RectTransform TierRoot => tierRoot;
    // 아이템 리스트 루트 바인딩을 외부에 노출한다.
    public RectTransform ListRoot => listRoot;
}
