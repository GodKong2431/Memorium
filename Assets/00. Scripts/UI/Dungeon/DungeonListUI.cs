using UnityEngine;
using System.Collections.Generic;

public class DungeonListUI : MonoBehaviour
{
    public static DungeonListUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    [Header("던전 데이터 설정")]
    public List<DungeonInfoData> dungeonDatas = new List<DungeonInfoData>();

    [Header("스크롤뷰 연결")]
    public Transform contentParent;
    public GameObject dungeonItemPrefab;

    private void Start()
    {
        InitializeList();
    }

    private void InitializeList()
    {
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        foreach (var data in dungeonDatas)
        {
            GameObject go = Instantiate(dungeonItemPrefab, contentParent);
            DungeonListItem item = go.GetComponent<DungeonListItem>();

            if (item != null)
            {
                item.Setup(data);
            }
        }
    }

    public void OnSelectDungeon(StageType selectedType)
    {
        Debug.Log($"[DungeonListUI] {selectedType} 선택 레벨 선택창으로 넘어감");

        GlobalPopupManager.Instance.OpenPopupMode(PopupMode.DungeonLevelSelect);

        DungeonLevelSelectUI.Instance.SetupDungeonType(selectedType);
    }
}