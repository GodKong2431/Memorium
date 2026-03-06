using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipItemUI : MonoBehaviour
{
    // 인스펙터에서 연결하는 순수 바인딩 컴포넌트.
    [SerializeField] private Button button;

    [SerializeField] private Slider mergeSlider;

    [SerializeField] private Image icon;

    [SerializeField] private TextMeshProUGUI levelText;

    [SerializeField] private Image tierPanel;

    [SerializeField] private RectTransform tierRoot;

    [SerializeField] private Image tierStar;

    [SerializeField] private TextMeshProUGUI currentCountText;

    [SerializeField] private TextMeshProUGUI needCountText;

    [SerializeField] private Image[] frames;

    public Button Button => button;
    public Slider MergeSlider => mergeSlider;
    public Image Icon => icon;
    public TextMeshProUGUI LevelText => levelText;
    public Image TierPanel => tierPanel;
    public RectTransform TierRoot => tierRoot;
    public Image TierStar => tierStar;
    public TextMeshProUGUI CurrentCountText => currentCountText;
    public TextMeshProUGUI NeedCountText => needCountText;
    public Image[] Frames => frames;
}

