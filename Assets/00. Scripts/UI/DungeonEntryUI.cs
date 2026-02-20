using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스크롤뷰 안에 배치되는 개별 던전 버튼 스크립트
/// </summary>
public class DungeonListItem : MonoBehaviour
{
    [Header("이 버튼의 던전 종류")]
    public DungeonType myDungeonType;

    [Header("UI 연결")]
    public Button btnSelectDungeon;

    private void Start()
    {
        if (btnSelectDungeon != null)
        {
            btnSelectDungeon.onClick.RemoveAllListeners();
            btnSelectDungeon.onClick.AddListener(OnClickDungeon);
        }
    }

    private void OnClickDungeon()
    {
        Debug.Log($"{myDungeonType} 던전이 선택 레벨 선택창");
    }
}