using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 모든 팝업의 종류를 관리하는 Enum입니다. (새로운 팝업이 생기면 여기에 이름만 추가하세요)
/// </summary>
public enum PopupMode
{
    BottomSheet,        // 하단 성장/장비 시트
    CharInfo,           // 캐릭터 정보창
    DungeonList,        // 던전 목록 팝업
    DungeonLevelSelect  // 던전 난이도 선택 팝업
}

/// <summary>
/// 인스펙터에서 팝업 모드와 켜질 패널들을 매칭하기 위한 구조체입니다.
/// </summary>
[Serializable]
public struct PopupMapping
{
    public PopupMode mode;
    public List<GameObject> activePanels; 
}

public class GlobalPopupManager : MonoBehaviour
{
    public static GlobalPopupManager Instance { get; private set; }

    [Header("Core UI")]
    public GameObject popupLayerRoot;
    public Button btnCommonClose;

    [Header("팝업 그룹 설정")]
    public List<PopupMapping> popupMappings = new List<PopupMapping>();

    private List<GameObject> allRegisteredPanels = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (var mapping in popupMappings)
        {
            foreach (var panel in mapping.activePanels)
            {
                if (panel != null && !allRegisteredPanels.Contains(panel))
                {
                    allRegisteredPanels.Add(panel);
                }
            }
        }
    }

    private void Start()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        ClosePopup();

        if (btnCommonClose != null)
        {
            btnCommonClose.onClick.RemoveAllListeners();
            btnCommonClose.onClick.AddListener(ClosePopup);
        }
    }

    /// <summary>
    /// 원하는 팝업 모드를 열고, 나머지는 전부 끔
    /// </summary>
    public void OpenPopupMode(PopupMode targetMode)
    {
        if (popupLayerRoot != null) popupLayerRoot.SetActive(true);

        foreach (var panel in allRegisteredPanels)
        {
            if (panel != null) panel.SetActive(false);
        }

        foreach (var mapping in popupMappings)
        {
            if (mapping.mode == targetMode)
            {
                foreach (var panel in mapping.activePanels)
                {
                    if (panel != null) panel.SetActive(true);
                }
                break;
            }
        }
    }

    /// <summary>
    /// 팝업 레이어를 닫고 모든 패널을 비활성화
    /// </summary>
    public void ClosePopup()
    {
        if (popupLayerRoot != null) popupLayerRoot.SetActive(false);

        foreach (var panel in allRegisteredPanels)
        {
            if (panel != null) panel.SetActive(false);
        }
    }

    public void ResetForSceneChange()
    {
        ClosePopup();
    }
}
