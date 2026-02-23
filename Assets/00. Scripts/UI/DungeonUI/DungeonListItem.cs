using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DungeonListItem : MonoBehaviour
{
    [Header("UI 연결")]
    public Image imgBackground;
    public TextMeshProUGUI textDungeonName;
    public Button btnSelect;

    private DungeonType myDungeonType;

    public void Setup(DungeonInfoData data)
    {
        myDungeonType = data.dungeonType;

        if (textDungeonName != null)
            textDungeonName.text = data.dungeonName;

        if (imgBackground != null && data.bgImage != null)
            imgBackground.sprite = data.bgImage;

        if (btnSelect != null)
        {
            btnSelect.onClick.RemoveAllListeners();
            btnSelect.onClick.AddListener(OnClickSelect);
        }
    }

    private void OnClickSelect()
    {
        DungeonListUI.Instance.OnSelectDungeon(myDungeonType);
    }
}