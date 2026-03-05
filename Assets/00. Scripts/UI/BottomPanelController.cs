using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ToggleGroup))]
public class BottomPanelController : MonoBehaviour
{
    // 하단 메인 탭 1개에 대한 설정값 묶음.
    [System.Serializable]
    private class TabConfig
    {
        [SerializeField] public string title; // 탭 선택 시 상단에 표시할 제목.
        [SerializeField] public Toggle mainToggle; // 하단 메인 탭 토글.
        [SerializeField] public bool lockMainSprite; // 메인 탭 공통 스프라이트 교체를 무시할지 여부.
        [SerializeField] public Sprite fixedSprite; // lockMainSprite 사용 시 고정으로 쓸 스프라이트.
        [SerializeField] public bool routeBySubMenu = true; // 서브 토글 선택에 따라 페이지를 바꿀지 여부.
        [SerializeField] public RectTransform subMenuRoot; // 시트에 붙여서 보여줄 서브메뉴 루트.
        [SerializeField] public List<Toggle> subToggles = new List<Toggle>(); // 탭별 서브 토글 목록.
        [SerializeField] public List<RectTransform> pages = new List<RectTransform>(); // 서브 인덱스에 대응되는 페이지 목록.
    }

    [Header("Sheet")]
    [SerializeField] private GameObject sheetPanel; // 하단 탭 선택 시 열고 닫을 시트 패널.
    [SerializeField] private TMP_Text titleText; // 시트 상단 제목 텍스트.
    [SerializeField] private RectTransform subMenuPos; // 활성 서브메뉴 루트를 붙일 위치.
    [SerializeField] private RectTransform contentsArea; // 활성 페이지를 붙일 위치.
    [SerializeField] private RectTransform subMenuContainer; // 비활성 서브메뉴를 보관할 원본 컨테이너.
    [SerializeField] private RectTransform contentsContainer; // 비활성 페이지를 보관할 원본 컨테이너.

    [Header("Tabs")]
    [SerializeField] private List<TabConfig> tabs = new List<TabConfig>(); // 하단 메인 탭 정의 목록.
    [SerializeField] private bool startClosed = true; // 시작 시 시트를 닫힌 상태로 둘지 여부.

    [Header("Main Toggle Sprite")]
    [SerializeField] private Sprite mainSpriteOff; // 메인 탭 비선택 스프라이트.
    [SerializeField] private Sprite mainSpriteOn; // 메인 탭 선택 스프라이트.

    [Header("Sub Menu Visual")]
    [SerializeField] private Color subOnColor = Color.white; // 서브 토글 선택 색상.
    [SerializeField] private Color subOffColor = new Color(0.65f, 0.65f, 0.65f, 1f); // 서브 토글 비선택 색상.

    private ToggleGroup toggleGroup; // 메인 탭 단일 선택을 제어하는 그룹.
    private int activeTab = -1; // 현재 선택된 메인 탭 인덱스.
    private int activeSub = -1; // 현재 선택된 서브 탭 인덱스.

    // 토글 초기화, 이벤트 연결, 시작 상태 적용을 수행한다.
    private void Awake()
    {
        toggleGroup = GetComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;

        InitMainToggles();
        InitSubToggles();
        BindMainEvents();
        BindSubEvents();

        if (startClosed)
            SelectMain(-1, true);
        else if (tabs.Count > 0)
            SelectMain(0, true);
    }

    // 메인 탭 토글을 그룹에 묶고 기본 상태를 초기화한다.
    private void InitMainToggles()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            TabConfig tab = tabs[i];
            tab.mainToggle.group = toggleGroup;
            tab.mainToggle.toggleTransition = Toggle.ToggleTransition.None;
            if (tab.mainToggle.graphic == tab.mainToggle.targetGraphic)
                tab.mainToggle.graphic = null;
            tab.mainToggle.SetIsOnWithoutNotify(false);
        }
    }

    // 서브 토글의 전이/그래픽 상태를 탭 설정에 맞춰 초기화한다.
    private void InitSubToggles()
    {
        for (int tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
        {
            TabConfig tab = tabs[tabIndex];
            bool routeBySub = tab.routeBySubMenu;

            for (int subIndex = 0; subIndex < tab.subToggles.Count; subIndex++)
            {
                Toggle toggle = tab.subToggles[subIndex];

                if (toggle.group == toggleGroup)
                    toggle.group = null;

                toggle.toggleTransition = Toggle.ToggleTransition.None;
                if (toggle.graphic == toggle.targetGraphic)
                    toggle.graphic = null;

                toggle.targetGraphic.canvasRenderer.SetAlpha(1f);

                if (!routeBySub)
                {
                    Color color = toggle.targetGraphic.color;
                    color.a = 1f;
                    toggle.targetGraphic.color = color;
                    continue;
                }

                toggle.transition = Selectable.Transition.None;
                toggle.SetIsOnWithoutNotify(false);
                toggle.targetGraphic.color = subOffColor;
            }
        }
    }

    // 메인 탭 onValueChanged 이벤트를 연결한다.
    private void BindMainEvents()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            TabConfig tab = tabs[i];
            int index = i;
            tab.mainToggle.onValueChanged.AddListener(isOn => OnMainChanged(index, isOn));
        }
    }

    // 메인 탭 토글 변경 시 시트 열기/닫기 흐름을 처리한다.
    private void OnMainChanged(int index, bool isOn)
    {
        if (index < 0 || index >= tabs.Count)
            return;

        if (isOn)
        {
            SelectMain(index);
            return;
        }

        if (activeTab == index && !toggleGroup.AnyTogglesOn())
            SelectMain(-1);
    }

    // 메인 탭 선택 상태를 갱신하고 해당 탭을 연다.
    private void SelectMain(int index, bool force = false)
    {
        if (!force && activeTab == index)
            return;

        activeTab = index;
        activeSub = -1;

        SyncMainState(index);
        RefreshMainVisual();

        if (index < 0)
        {
            CloseSheet();
            return;
        }

        OpenTab(index);
    }

    // 선택된 메인 탭만 On 상태로 동기화한다.
    private void SyncMainState(int selectedIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
            tabs[i].mainToggle.SetIsOnWithoutNotify(i == selectedIndex);
    }

    // 메인 탭의 선택/비선택 스프라이트를 갱신한다.
    private void RefreshMainVisual()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            TabConfig tab = tabs[i];
            Image mainImage = tab.mainToggle.targetGraphic as Image;

            bool isSelected = i == activeTab;
            if (tab.lockMainSprite)
                mainImage.sprite = tab.fixedSprite;
            else
                mainImage.sprite = isSelected ? mainSpriteOn : mainSpriteOff;

            mainImage.canvasRenderer.SetAlpha(1f);
            mainImage.color = Color.white;
            tab.mainToggle.transition = Selectable.Transition.None;
        }
    }

    // 선택된 탭의 서브메뉴/페이지를 시트 영역으로 이동해 표시한다.
    private void OpenTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabs.Count)
        {
            CloseSheet();
            return;
        }

        TabConfig tab = tabs[tabIndex];

        SetSheet(true);
        SetTitle(GetTitle(tab));

        MoveChildren(subMenuPos, subMenuContainer);
        MoveChildren(contentsArea, contentsContainer);

        AttachPage(tab.subMenuRoot, subMenuPos);
        EnsureSubVisible(tab);
        OpenFirstSub(tab);
    }

    // 시트를 닫고 현재 열린 서브메뉴/페이지를 원래 컨테이너로 되돌린다.
    private void CloseSheet()
    {
        MoveChildren(subMenuPos, subMenuContainer);
        MoveChildren(contentsArea, contentsContainer);
        SetTitle(string.Empty);
        SetSheet(false);
    }

    // 서브 토글을 사용하는 탭들에 이벤트를 연결한다.
    private void BindSubEvents()
    {
        for (int tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
        {
            TabConfig tab = tabs[tabIndex];
            if (!tab.routeBySubMenu)
                continue;

            for (int subIndex = 0; subIndex < tab.subToggles.Count; subIndex++)
            {
                Toggle toggle = tab.subToggles[subIndex];
                int cachedTab = tabIndex;
                int cachedSub = subIndex;
                toggle.onValueChanged.AddListener(isOn => OnSubChanged(cachedTab, cachedSub, isOn));
            }
        }
    }

    // 탭 오픈 시 기본으로 보여줄 첫 서브 메뉴/페이지를 결정한다.
    private void OpenFirstSub(TabConfig tab)
    {
        if (!tab.routeBySubMenu)
        {
            ShowSubPage(tab, 0);
            return;
        }

        if (tab.subToggles.Count > 0)
        {
            SelectSub(tab, 0, true);
            return;
        }

        activeSub = -1;
        RefreshSubVisual(tab, -1);
        ShowSubPage(tab, 0);
    }

    // 서브 토글 변경 시 콘텐츠 페이지 전환을 처리한다.
    private void OnSubChanged(int tabIndex, int subIndex, bool isOn)
    {
        if (tabIndex != activeTab)
            return;
        if (tabIndex < 0 || tabIndex >= tabs.Count)
            return;

        TabConfig tab = tabs[tabIndex];
        if (!tab.routeBySubMenu)
            return;

        if (isOn)
        {
            SelectSub(tab, subIndex);
            return;
        }

        if (activeSub == subIndex && !HasSubOn(tab))
            SelectSub(tab, 0, true);
    }

    // 서브 선택 상태와 페이지를 함께 갱신한다.
    private void SelectSub(TabConfig tab, int subIndex, bool force = false)
    {
        if (!force && activeSub == subIndex)
            return;

        activeSub = subIndex;
        SyncSubState(tab, subIndex);
        RefreshSubVisual(tab, subIndex);
        ShowSubPage(tab, subIndex);
    }

    // 선택된 서브 토글만 On 상태로 동기화한다.
    private void SyncSubState(TabConfig tab, int selectedIndex)
    {
        for (int i = 0; i < tab.subToggles.Count; i++)
            tab.subToggles[i].SetIsOnWithoutNotify(i == selectedIndex);
    }

    // 현재 탭에서 하나라도 서브 토글이 선택됐는지 확인한다.
    private static bool HasSubOn(TabConfig tab)
    {
        for (int i = 0; i < tab.subToggles.Count; i++)
        {
            if (tab.subToggles[i].isOn)
                return true;
        }

        return false;
    }

    // 서브 토글의 선택/비선택 색상을 갱신한다.
    private void RefreshSubVisual(TabConfig tab, int selectedIndex)
    {
        for (int i = 0; i < tab.subToggles.Count; i++)
            tab.subToggles[i].targetGraphic.color = i == selectedIndex ? subOnColor : subOffColor;
    }

    // 서브 인덱스에 맞는 페이지를 contentsArea에 붙여 표시한다.
    private void ShowSubPage(TabConfig tab, int subIndex)
    {
        MoveChildren(contentsArea, contentsContainer);

        if (tab.pages.Count == 0)
            return;

        int pageIndex = subIndex;
        if (pageIndex < 0 || pageIndex >= tab.pages.Count)
            pageIndex = 0;

        AttachPage(tab.pages[pageIndex], contentsArea);
    }

    // 서브 토글 그래픽의 알파를 보이는 상태로 강제한다.
    private static void EnsureSubVisible(TabConfig tab)
    {
        for (int i = 0; i < tab.subToggles.Count; i++)
        {
            Graphic targetGraphic = tab.subToggles[i].targetGraphic;
            targetGraphic.canvasRenderer.SetAlpha(1f);
            Color color = targetGraphic.color;
            color.a = 1f;
            targetGraphic.color = color;
        }
    }

    // 탭 제목이 비어 있으면 오브젝트 이름을 기반으로 제목을 만든다.
    private static string GetTitle(TabConfig tab)
    {
        if (!string.IsNullOrEmpty(tab.title))
            return tab.title;

        return tab.mainToggle.gameObject.name.Replace("(Btn)", string.Empty).Trim();
    }

    // 시트 패널의 활성 상태를 변경한다.
    private void SetSheet(bool visible)
    {
        sheetPanel.SetActive(visible);
    }

    // 시트 상단 제목 텍스트를 갱신한다.
    private void SetTitle(string title)
    {
        titleText.text = title;
    }

    // from의 자식들을 to로 이동시키고 비활성화한다.
    private static void MoveChildren(RectTransform from, RectTransform to)
    {
        while (from.childCount > 0)
        {
            Transform child = from.GetChild(0);
            child.SetParent(to, false);
            child.gameObject.SetActive(false);
        }
    }

    // 페이지를 target으로 이동시키고 활성화한다.
    private static void AttachPage(RectTransform page, RectTransform target)
    {
        page.SetParent(target, false);
        page.SetAsLastSibling();
        page.gameObject.SetActive(true);
    }
}
