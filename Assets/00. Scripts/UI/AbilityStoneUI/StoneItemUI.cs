using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class StoneItemUI : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image frameImage;
    [SerializeField] private TextMeshProUGUI gradeText;
    [SerializeField] private RectTransform[] slotRoots = new RectTransform[3];
    [SerializeField] private TextMeshProUGUI[] slotTexts = new TextMeshProUGUI[3];
    [SerializeField] private GameObject lockObject;

    private Image[] slotImages;

    public Button Button => button;
    public Image FrameImage => frameImage;
    public TextMeshProUGUI GradeText => gradeText;
    public TextMeshProUGUI[] SlotTexts => slotTexts;
    public GameObject LockObject => lockObject;

    public Image GetSlotImage(int index)
    {
        if (slotRoots == null || index < 0 || index >= slotRoots.Length || slotRoots[index] == null)
        {
            return null;
        }

        slotImages ??= new Image[slotRoots.Length];
        slotImages[index] ??= slotRoots[index].GetComponent<Image>();
        return slotImages[index];
    }

    public void SetLocked(bool isLocked)
    {
        if (lockObject != null)
        {
            lockObject.SetActive(isLocked);
        }
    }
}
