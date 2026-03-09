using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 장비 아이템 UI 오브젝트의 인스펙터 바인딩 홀더다.
public class EquipItemUI : MonoBehaviour
{
    // 장비 아이템 클릭 버튼이다.
    [SerializeField] private Button button;
    // 합성 진행도를 표시하는 슬라이더다.
    [SerializeField] private Slider mergeSlider;
    // 장비 아이콘 이미지다.
    [SerializeField] private Image icon;
    // 레벨 텍스트 라벨이다.
    [SerializeField] private TextMeshProUGUI levelText;
    // 티어 패널 배경 이미지다.
    [SerializeField] private Image tierPanel;
    // 티어 별 아이콘 부모 트랜스폼이다.
    [SerializeField] private RectTransform tierRoot;
    // 기본 티어 별 이미지 템플릿이다.
    [SerializeField] private Image tierStar;
    // 현재 보유 개수 텍스트 라벨이다.
    [SerializeField] private TextMeshProUGUI currentCountText;
    // 필요 개수 텍스트 라벨이다.
    [SerializeField] private TextMeshProUGUI needCountText;
    // 희귀도/순서 표현용 프레임 이미지 배열이다.
    [SerializeField] private Image[] frames;

    // 버튼 바인딩을 외부에 노출한다.
    public Button Button => button;
    // 합성 슬라이더 바인딩을 외부에 노출한다.
    public Slider MergeSlider => mergeSlider;
    // 아이콘 이미지 바인딩을 외부에 노출한다.
    public Image Icon => icon;
    // 레벨 텍스트 바인딩을 외부에 노출한다.
    public TextMeshProUGUI LevelText => levelText;
    // 티어 패널 바인딩을 외부에 노출한다.
    public Image TierPanel => tierPanel;
    // 티어 루트 트랜스폼 바인딩을 외부에 노출한다.
    public RectTransform TierRoot => tierRoot;
    // 기본 별 이미지 바인딩을 외부에 노출한다.
    public Image TierStar => tierStar;
    // 현재 개수 텍스트 바인딩을 외부에 노출한다.
    public TextMeshProUGUI CurrentCountText => currentCountText;
    // 필요 개수 텍스트 바인딩을 외부에 노출한다.
    public TextMeshProUGUI NeedCountText => needCountText;
    // 프레임 이미지 배열 바인딩을 외부에 노출한다.
    public Image[] Frames => frames;
}
