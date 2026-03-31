using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[RequireComponent(typeof(ToggleGroup))]
public class BottomPanelController : MonoBehaviour
{
#region Serialized Types
    [System.Serializable]
    private class TabConfig
    {
        [Header("Main")]
        [SerializeField] public string title;
        [SerializeField] public Toggle mainToggle;
        [SerializeField] public bool lockMainSprite;
        [SerializeField] public Sprite fixedSprite;

        [Header("Sub")]
        [SerializeField] public bool routeBySubMenu = true;
        [SerializeField] public RectTransform subMenuRoot;
        [SerializeField] public List<Toggle> subToggles = new List<Toggle>();
        [SerializeField] public List<RectTransform> pages = new List<RectTransform>();
    }
#endregion

#region Runtime Types
    // 페이지 안쪽 ScrollRect의 기준 높이를 기억해 두는 캐시다.
    private sealed class ExternalPageToggleBinding
    {
        public Toggle toggle;
        public RectTransform page;
        public UnityAction<bool> listener;
    }

    private sealed class PageResizeCache
    {
        public RectTransform page;
        public readonly List<ScrollRectResizeCache> scrollRects = new List<ScrollRectResizeCache>();
    }

    // 개별 ScrollRect의 원래 높이를 따로 저장한다.
    private sealed class ScrollRectResizeCache
    {
        public RectTransform rect;
        public bool isPageRoot;
        public float topInset;
        public float bottomInset;
    }
#endregion

#region Serialized Fields
    [Header("References - Sheet")]
    [SerializeField] private GameObject sheetPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private RectTransform subMenuPos;
    [SerializeField] private RectTransform contentsArea;
    [SerializeField] private RectTransform subMenuContainer;
    [SerializeField] private RectTransform contentsContainer;
    [SerializeField] private RectTransform topPanelRect;

    [Header("References - Gesture")]
    [SerializeField] private RectTransform headerGestureArea;
    [SerializeField] private SheetHeaderGestureHandle headerGestureHandle;
    [SerializeField] private RectTransform gestureBlockerContainer;
    [SerializeField] private List<RectTransform> gestureBlockerRoots = new List<RectTransform>();

    [Header("References - Tabs")]
    [SerializeField] private List<TabConfig> tabs = new List<TabConfig>();
    [SerializeField] private bool startClosed = true;

    [Header("Visual - Main Tab")]
    [SerializeField] private Sprite mainSpriteOff;
    [SerializeField] private Sprite mainSpriteOn;

    [Header("Visual - Sub Menu")]
    [SerializeField] private Color subOnColor = Color.white;
    [SerializeField] private Color subOffColor = new Color(0.65f, 0.65f, 0.65f, 1f);

    [Header("Height - Base")]
    [SerializeField, Min(0f)] private float sheetDefaultHeight;
    [SerializeField, Min(0f)] private float minSheetHeight = 360f;
    [SerializeField, Min(0f)] private float maxSheetHeightOverride;
    [SerializeField, Min(0f)] private float maxSheetExtraHeight = 400f;
    [SerializeField] private float topPanelSpacing;

    [Header("Height - Snap Threshold")]
    [SerializeField, Min(0f)] private float closeSnapHeight;
    [SerializeField, Min(0f)] private float expandSnapHeight;

    [Header("Gesture")]
    [SerializeField, Min(0f)] private float headerGestureHeight = 120f;
    [SerializeField, Min(0f)] private float doubleTapThreshold = 0.25f;
#endregion

#region Runtime Fields
    private readonly List<PageResizeCache> pageResizeCaches = new List<PageResizeCache>();
    private readonly List<ExternalPageToggleBinding> externalPageToggleBindings = new List<ExternalPageToggleBinding>();

    private ToggleGroup toggleGroup;
    private Canvas parentCanvas;
    private RectTransform sheetRect;
    private Coroutine singleTapCoroutine;
    private int activeTab = -1;
    private int activeSub = -1;
    private RectTransform activeStandalonePage;

    private float initialSheetHeight;
    private float contentsAreaHeightOffset;
    private float contentsContainerHeightOffset;

    private float lastHeaderTapTime = -10f;
    private bool waitingForSecondTap;
    private bool isDraggingHeader;
    private bool suppressNextPointerUpTap;
#endregion

#region Public State
    public bool IsSheetOpen =>
        sheetPanel != null &&
        sheetPanel.activeSelf &&
        HasActiveSheetPage();
#endregion

#region Unity Lifecycle
    // 초기 참조와 이벤트를 한 번에 준비한다.
    private void Awake()
    {
        toggleGroup = GetComponent<ToggleGroup>();
        toggleGroup.allowSwitchOff = true;

        InitializeSheet();
        SetupMainToggles();
        SetupSubToggles();

        if (startClosed)
            SelectMain(-1, true);
        else if (tabs.Count > 0)
            SelectMain(0, true);
    }

    // 비활성화될 때 입력 대기 상태를 정리한다.
    private void OnDisable()
    {
        ResetHeaderGestureState();
    }
#endregion

#region Initialization
    // 시트 크기 계산과 제스처 입력에 필요한 참조를 준비한다.
    private void InitializeSheet()
    {
        sheetRect = sheetPanel != null ? sheetPanel.transform as RectTransform : null;
        if (sheetRect == null)
            return;

        parentCanvas = sheetRect.GetComponentInParent<Canvas>();
        initialSheetHeight = Mathf.Max(minSheetHeight, ResolveDefaultSheetHeight());
        contentsAreaHeightOffset = GetHeightOffset(contentsArea);
        contentsContainerHeightOffset = GetHeightOffset(contentsContainer);

        SetupHeaderGestureArea();
        ApplySheetHeight(initialSheetHeight);
    }

    // 기본 높이는 인스펙터 값이 우선이고 없으면 현재 높이를 사용한다.
    private float ResolveDefaultSheetHeight()
    {
        float currentHeight = sheetRect != null ? sheetRect.sizeDelta.y : 0f;
        return sheetDefaultHeight > 0f ? sheetDefaultHeight : currentHeight;
    }

    // 시트와 내부 영역의 높이 차이를 미리 계산해 둔다.
    private float GetHeightOffset(RectTransform target)
    {
        return target == null ? 0f : initialSheetHeight - target.sizeDelta.y;
    }


    // 헤더 제스처 영역은 scene에 직렬화된 참조만 사용한다.
    private void SetupHeaderGestureArea()
    {
        if (headerGestureArea == null)
        {
            Debug.LogWarning("BottomPanelController: Header Gesture Area가 비어 있습니다.", this);
            return;
        }

        headerGestureArea.anchorMin = new Vector2(0f, 1f);
        headerGestureArea.anchorMax = new Vector2(1f, 1f);
        headerGestureArea.pivot = new Vector2(0.5f, 1f);
        headerGestureArea.anchoredPosition = Vector2.zero;
        headerGestureArea.sizeDelta = new Vector2(0f, headerGestureHeight);
        headerGestureArea.SetAsLastSibling();

        if (headerGestureHandle == null)
        {
            Debug.LogWarning("BottomPanelController: Header Gesture Handle이 연결되지 않았습니다.", this);
            return;
        }

        if (headerGestureHandle.transform != headerGestureArea)
        {
            Debug.LogWarning("BottomPanelController: Header Gesture Handle 참조가 Header Gesture Area와 다릅니다.", this);
            return;
        }

        headerGestureHandle.Bind(this);
    }
#endregion

#region Tabs
    // 메인 탭 토글의 기본 상태와 이벤트를 준비한다.
    private void SetupMainToggles()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            TabConfig tab = tabs[i];
            if (tab.mainToggle == null)
                continue;

            tab.mainToggle.group = toggleGroup;
            tab.mainToggle.toggleTransition = Toggle.ToggleTransition.None;
            tab.mainToggle.transition = Selectable.Transition.None;

            if (tab.mainToggle.graphic == tab.mainToggle.targetGraphic)
                tab.mainToggle.graphic = null;

            tab.mainToggle.SetIsOnWithoutNotify(false);

            int capturedIndex = i;
            tab.mainToggle.onValueChanged.AddListener(isOn => OnMainChanged(capturedIndex, isOn));
        }
    }

    // 서브 토글의 시각 상태와 이벤트를 초기화한다.
    private void SetupSubToggles()
    {
        for (int tabIndex = 0; tabIndex < tabs.Count; tabIndex++)
        {
            TabConfig tab = tabs[tabIndex];

            for (int subIndex = 0; subIndex < tab.subToggles.Count; subIndex++)
            {
                Toggle toggle = tab.subToggles[subIndex];
                PrepareSubToggle(toggle, tab.routeBySubMenu);

                if (!tab.routeBySubMenu || toggle == null)
                    continue;

                int capturedTabIndex = tabIndex;
                int capturedSubIndex = subIndex;
                toggle.onValueChanged.AddListener(isOn => OnSubChanged(capturedTabIndex, capturedSubIndex, isOn));
            }
        }
    }

    // 서브 토글 한 개의 공통 설정을 맞춘다.
    private void PrepareSubToggle(Toggle toggle, bool routeBySubMenu)
    {
        if (toggle == null)
            return;

        if (toggle.group == toggleGroup)
            toggle.group = null;

        toggle.toggleTransition = Toggle.ToggleTransition.None;

        if (toggle.graphic == toggle.targetGraphic)
            toggle.graphic = null;

        if (toggle.targetGraphic != null)
        {
            toggle.targetGraphic.canvasRenderer.SetAlpha(1f);

            Color color = toggle.targetGraphic.color;
            color.a = 1f;
            toggle.targetGraphic.color = routeBySubMenu ? subOffColor : color;
        }

        if (!routeBySubMenu)
            return;

        toggle.transition = Selectable.Transition.None;
        toggle.SetIsOnWithoutNotify(false);
    }

    // 메인 탭 토글 변경 시 시트를 열거나 닫는다.
    private void OnMainChanged(int index, bool isOn)
    {
        if (!IsValidTabIndex(index))
            return;

        if (isOn)
        {
            SelectMain(index);
            return;
        }

        if (activeTab == index && (toggleGroup == null || !toggleGroup.AnyTogglesOn()))
            SelectMain(-1);
    }

    // 메인 탭 선택 상태를 갱신하고 필요한 화면을 연다.
    private void SelectMain(int index, bool force = false)
    {
        int nextIndex = IsValidTabIndex(index) ? index : -1;
        if (!force && activeTab == nextIndex && activeStandalonePage == null)
            return;

        activeTab = nextIndex;
        activeSub = -1;
        activeStandalonePage = null;

        if (waitingForSecondTap)
        {
            CancelPendingSingleTap();
            waitingForSecondTap = false;
            lastHeaderTapTime = -10f;
        }

        for (int i = 0; i < tabs.Count; i++)
        {
            Toggle toggle = tabs[i].mainToggle;
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(i == activeTab);
        }

        RefreshMainSelection();
        RefreshExternalPageToggles();

        if (activeTab < 0)
        {
            CloseSheet();
            return;
        }

        OpenTab(tabs[activeTab]);
    }

    // 유효한 메인 탭 인덱스인지 확인한다.
    private bool IsValidTabIndex(int index)
    {
        return index >= 0 && index < tabs.Count;
    }

    // 메인 탭 선택 시각 상태를 갱신한다.
    private void RefreshMainSelection()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            TabConfig tab = tabs[i];
            if (tab.mainToggle == null)
                continue;

            Image mainImage = tab.mainToggle.targetGraphic as Image;
            if (mainImage == null)
                continue;

            bool isSelected = i == activeTab;
            if (tab.lockMainSprite)
                mainImage.sprite = tab.fixedSprite;
            else
                mainImage.sprite = isSelected ? mainSpriteOn : mainSpriteOff;

            mainImage.canvasRenderer.SetAlpha(1f);
            mainImage.color = Color.white;
        }
    }

    // 선택된 탭의 서브메뉴와 페이지를 시트 영역에 붙인다.
    private void OpenTab(TabConfig tab)
    {
        bool wasVisible = sheetPanel != null && sheetPanel.activeSelf;

        SetSheetVisible(true);
        if (!wasVisible)
            ResetSheetHeight();

        PrepareTabLayout(tab, 0);
        OpenFirstSub(tab);
    }

    public bool ShowManagedPage(RectTransform page)
    {
        if (page == null)
            return false;

        if (TryFindManagedPage(page, out int tabIndex, out int pageIndex))
        {
            OpenManagedPage(tabIndex, pageIndex);
            return true;
        }

        if (TryResolveStandaloneManagedPage(page, out RectTransform standalonePage))
        {
            OpenStandaloneManagedPage(standalonePage);
            return true;
        }

        return false;
    }

    public void OpenManagedContent(RectTransform page)
    {
        ShowManagedPage(page);
    }

    public void RegisterExternalPageToggle(Toggle toggle, RectTransform page)
    {
        if (toggle == null || page == null)
            return;

        UnregisterExternalPageToggle(toggle);

        toggle.group = null;
        toggle.toggleTransition = Toggle.ToggleTransition.None;
        toggle.transition = Selectable.Transition.None;
        if (toggle.graphic == toggle.targetGraphic)
            toggle.graphic = null;

        ExternalPageToggleBinding binding = new ExternalPageToggleBinding
        {
            toggle = toggle,
            page = page
        };
        binding.listener = isOn => OnExternalPageToggleChanged(binding, isOn);
        externalPageToggleBindings.Add(binding);
        toggle.onValueChanged.AddListener(binding.listener);
        SyncExternalPageToggle(binding);
    }

    public void UnregisterExternalPageToggle(Toggle toggle)
    {
        if (toggle == null)
            return;

        for (int i = externalPageToggleBindings.Count - 1; i >= 0; i--)
        {
            ExternalPageToggleBinding binding = externalPageToggleBindings[i];
            if (binding.toggle != toggle)
                continue;

            if (binding.listener != null)
                binding.toggle.onValueChanged.RemoveListener(binding.listener);

            externalPageToggleBindings.RemoveAt(i);
        }
    }

    public bool IsManagedPageRegistered(RectTransform page)
    {
        return page != null &&
               (TryFindManagedPage(page, out _, out _) ||
                TryResolveStandaloneManagedPage(page, out _));
    }

    public void ResetForSceneChange()
    {
        SelectMain(-1, true);
    }

    private bool TryFindManagedPage(RectTransform page, out int tabIndex, out int pageIndex)
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            TabConfig tab = tabs[i];
            for (int j = 0; j < tab.pages.Count; j++)
            {
                RectTransform registeredPage = tab.pages[j];
                if (registeredPage == null)
                    continue;

                if (registeredPage != page && !page.IsChildOf(registeredPage))
                    continue;

                tabIndex = i;
                pageIndex = j;
                return true;
            }
        }

        tabIndex = -1;
        pageIndex = -1;
        return false;
    }

    private bool TryResolveStandaloneManagedPage(RectTransform page, out RectTransform standalonePage)
    {
        standalonePage = null;
        if (page == null)
            return false;

        Transform current = page;
        while (current != null)
        {
            RectTransform currentRect = current as RectTransform;
            if (currentRect == null)
            {
                current = current.parent;
                continue;
            }

            if (currentRect == contentsArea || currentRect == contentsContainer)
                break;

            if (TryFindManagedPage(currentRect, out _, out _))
                return false;

            if (currentRect.GetComponent<BottomPanelManagedPage>() != null &&
                IsStandaloneManagedPageRoot(currentRect))
            {
                standalonePage = currentRect;
                return true;
            }

            current = current.parent;
        }

        return false;
    }

    private bool IsStandaloneManagedPageRoot(RectTransform page)
    {
        if (page == null)
            return false;

        RectTransform parent = page.parent as RectTransform;
        return parent == contentsArea || parent == contentsContainer;
    }

    private void OpenManagedPage(int tabIndex, int pageIndex)
    {
        if (!IsValidTabIndex(tabIndex))
            return;

        CancelPendingSingleTap();
        waitingForSecondTap = false;
        lastHeaderTapTime = -10f;
        suppressNextPointerUpTap = false;

        activeTab = tabIndex;
        activeSub = -1;
        activeStandalonePage = null;

        for (int i = 0; i < tabs.Count; i++)
        {
            Toggle toggle = tabs[i].mainToggle;
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(i == activeTab);
        }

        RefreshMainSelection();
        RefreshExternalPageToggles();

        TabConfig tab = tabs[activeTab];
        bool wasVisible = sheetPanel != null && sheetPanel.activeSelf;
        SetSheetVisible(true);
        if (!wasVisible)
            ResetSheetHeight();

        PrepareTabLayout(tab, pageIndex);

        if (tab.routeBySubMenu && tab.subToggles.Count > 0)
        {
            int subIndex = Mathf.Clamp(pageIndex, 0, tab.subToggles.Count - 1);
            SelectSub(tab, subIndex, true);
            return;
        }

        RefreshSubSelection(tab, -1);
        ShowSubPage(tab, pageIndex);
    }

    private void OpenStandaloneManagedPage(RectTransform page)
    {
        if (page == null)
            return;

        CancelPendingSingleTap();
        waitingForSecondTap = false;
        lastHeaderTapTime = -10f;
        suppressNextPointerUpTap = false;

        activeTab = -1;
        activeSub = -1;
        activeStandalonePage = page;

        for (int i = 0; i < tabs.Count; i++)
        {
            Toggle toggle = tabs[i].mainToggle;
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(false);
        }

        RefreshMainSelection();
        RefreshExternalPageToggles();

        bool wasVisible = sheetPanel != null && sheetPanel.activeSelf;
        SetSheetVisible(true);
        if (!wasVisible)
            ResetSheetHeight();

        PrepareStandalonePageLayout(page);
        AttachRect(page, contentsArea, true);
        RefreshResizableContent();
    }

    // 시트를 닫고 활성 콘텐츠를 원래 컨테이너로 되돌린다.
    private void PrepareTabLayout(TabConfig tab, int pageIndex)
    {
        RectTransform page = GetPage(tab, pageIndex);
        BottomPanelManagedPage managedPage = GetManagedPage(page);
        RectTransform subMenuRoot = ResolveSubMenuRoot(tab, managedPage);

        SetTitle(GetPageTitle(tab, pageIndex, managedPage));

        MoveChildren(subMenuPos, subMenuContainer);
        MoveChildren(contentsArea, contentsContainer);

        if (ShouldShowSubMenu(tab, pageIndex, managedPage, subMenuRoot))
        {
            AttachRect(subMenuRoot, subMenuPos, false);
            EnsureSubVisible(tab);
        }
    }

    private void PrepareStandalonePageLayout(RectTransform page)
    {
        BottomPanelManagedPage managedPage = GetManagedPage(page);
        RectTransform subMenuRoot = managedPage != null ? managedPage.SubMenuRootOverride : null;

        SetTitle(GetStandalonePageTitle(page, managedPage));

        MoveChildren(subMenuPos, subMenuContainer);
        MoveChildren(contentsArea, contentsContainer);

        if (managedPage != null && managedPage.ShowSubMenu && subMenuRoot != null)
            AttachRect(subMenuRoot, subMenuPos, false);
    }

    private static RectTransform ResolveSubMenuRoot(TabConfig tab, BottomPanelManagedPage managedPage)
    {
        if (managedPage != null && managedPage.SubMenuRootOverride != null)
            return managedPage.SubMenuRootOverride;

        return tab != null ? tab.subMenuRoot : null;
    }

    private static bool ShouldShowSubMenu(TabConfig tab, int pageIndex, BottomPanelManagedPage managedPage, RectTransform subMenuRoot)
    {
        if (tab == null || subMenuRoot == null)
            return false;

        if (managedPage != null)
            return managedPage.ShowSubMenu;

        if (tab.routeBySubMenu)
            return true;

        return pageIndex <= 0;
    }

    private static string GetPageTitle(TabConfig tab, int pageIndex, BottomPanelManagedPage managedPage)
    {
        if (tab == null)
            return string.Empty;

        string fallbackTitle = GetTabTitle(tab);
        if (managedPage != null && !string.IsNullOrWhiteSpace(managedPage.PageTitle))
            return managedPage.PageTitle;

        if (tab.routeBySubMenu || pageIndex <= 0 || pageIndex >= tab.pages.Count)
            return fallbackTitle;

        RectTransform page = GetPage(tab, pageIndex);
        if (page == null)
            return fallbackTitle;

        string pageName = GetDisplayPageName(page);
        return string.IsNullOrEmpty(pageName) ? fallbackTitle : pageName;
    }

    private static string GetStandalonePageTitle(RectTransform page, BottomPanelManagedPage managedPage)
    {
        if (managedPage != null && !string.IsNullOrWhiteSpace(managedPage.PageTitle))
            return managedPage.PageTitle;

        return GetDisplayPageName(page);
    }

    private static string GetDisplayPageName(RectTransform page)
    {
        if (page == null)
            return string.Empty;

        string pageName = page.gameObject.name;
        if (pageName.EndsWith("Contents"))
            pageName = pageName.Substring(0, pageName.Length - "Contents".Length);

        return pageName.Replace("(ScrollView)", string.Empty).Trim();
    }

    private void CloseSheet()
    {
        activeStandalonePage = null;
        MoveChildren(subMenuPos, subMenuContainer);
        MoveChildren(contentsArea, contentsContainer);
        SetTitle(string.Empty);
        SetSheetVisible(false);
        ResetSheetHeight();
        RefreshExternalPageToggles();
    }

    // 탭 오픈 시 기본 서브 페이지를 결정한다.
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
        RefreshSubSelection(tab, -1);
        ShowSubPage(tab, 0);
    }

    // 서브 토글 변경 시 현재 탭 페이지를 전환한다.
    private void OnSubChanged(int tabIndex, int subIndex, bool isOn)
    {
        if (tabIndex != activeTab || !IsValidTabIndex(tabIndex))
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

    // 서브 탭 선택 상태와 페이지를 함께 갱신한다.
    private void SelectSub(TabConfig tab, int subIndex, bool force = false)
    {
        if (tab == null)
            return;

        if (tab.subToggles.Count == 0)
        {
            activeSub = -1;
            RefreshSubSelection(tab, -1);
            ShowSubPage(tab, 0);
            return;
        }

        int nextIndex = Mathf.Clamp(subIndex, 0, tab.subToggles.Count - 1);
        if (!force && activeSub == nextIndex)
            return;

        activeSub = nextIndex;

        for (int i = 0; i < tab.subToggles.Count; i++)
        {
            Toggle toggle = tab.subToggles[i];
            if (toggle != null)
                toggle.SetIsOnWithoutNotify(i == activeSub);
        }

        RefreshSubSelection(tab, activeSub);
        ShowSubPage(tab, activeSub);
        RefreshExternalPageToggles();
    }

    // 서브 토글 선택 색상을 갱신한다.
    private void RefreshSubSelection(TabConfig tab, int selectedIndex)
    {
        if (tab == null)
            return;

        for (int i = 0; i < tab.subToggles.Count; i++)
        {
            Toggle toggle = tab.subToggles[i];
            if (toggle == null || toggle.targetGraphic == null)
                continue;

            toggle.targetGraphic.color = i == selectedIndex ? subOnColor : subOffColor;
        }
    }

    // 현재 탭에서 선택된 서브 토글이 있는지 확인한다.
    private static bool HasSubOn(TabConfig tab)
    {
        for (int i = 0; i < tab.subToggles.Count; i++)
        {
            Toggle toggle = tab.subToggles[i];
            if (toggle != null && toggle.isOn)
                return true;
        }

        return false;
    }

    // 선택된 서브 인덱스에 맞는 페이지를 시트에 붙인다.
    private void ShowSubPage(TabConfig tab, int subIndex)
    {
        MoveChildren(contentsArea, contentsContainer);

        if (tab == null || tab.pages.Count == 0)
            return;

        int pageIndex = subIndex;
        if (pageIndex < 0 || pageIndex >= tab.pages.Count)
            pageIndex = 0;

        AttachRect(GetPage(tab, pageIndex), contentsArea, true);
        RefreshResizableContent();
    }

    // 서브 토글 그래픽이 항상 보이도록 알파를 맞춘다.
    private static void EnsureSubVisible(TabConfig tab)
    {
        for (int i = 0; i < tab.subToggles.Count; i++)
        {
            Toggle toggle = tab.subToggles[i];
            if (toggle == null || toggle.targetGraphic == null)
                continue;

            toggle.targetGraphic.canvasRenderer.SetAlpha(1f);

            Color color = toggle.targetGraphic.color;
            color.a = 1f;
            toggle.targetGraphic.color = color;
        }
    }

    // 인스펙터 제목이 비어 있으면 오브젝트 이름으로 제목을 만든다.
    private static string GetTabTitle(TabConfig tab)
    {
        if (!string.IsNullOrEmpty(tab.title))
            return tab.title;

        if (tab.mainToggle == null)
            return string.Empty;

        return tab.mainToggle.gameObject.name.Replace("(Btn)", string.Empty).Trim();
    }

    // 시트 오브젝트의 활성 상태를 바꾼다.
    private void SetSheetVisible(bool visible)
    {
        if (sheetPanel != null && sheetPanel.activeSelf != visible)
            sheetPanel.SetActive(visible);
    }

    // 시트 제목을 갱신한다.
    private void SetTitle(string title)
    {
        if (titleText != null)
            titleText.text = title;
    }
#endregion

#region Gesture
    // 헤더 터치 시작 시 싱글 탭 대기만 정리한다.
    public void HandleHeaderPointerDown()
    {
        if (!HasActiveSheetPage() || sheetRect == null || IsHeaderGestureBlocked())
        {
            ResetHeaderGestureState();
            return;
        }

        if (waitingForSecondTap)
            CancelPendingSingleTap();
    }

    // 헤더 터치 종료 시 싱글 탭 닫기 또는 더블 탭 확장을 판정한다.
    public void HandleHeaderPointerUp()
    {
        if (!HasActiveSheetPage() || sheetRect == null || sheetPanel == null || !sheetPanel.activeSelf || IsHeaderGestureBlocked())
        {
            ResetHeaderGestureState();
            return;
        }

        if (isDraggingHeader || ConsumeSuppressedPointerUp())
            return;

        float currentTime = Time.unscaledTime;
        if (waitingForSecondTap && currentTime - lastHeaderTapTime <= doubleTapThreshold)
        {
            CancelPendingSingleTap();
            waitingForSecondTap = false;
            lastHeaderTapTime = -10f;
            ApplySheetHeight(GetMaxSheetHeight());
            return;
        }

        waitingForSecondTap = true;
        lastHeaderTapTime = currentTime;
        QueueSingleTapClose();
    }

    // 헤더 드래그가 시작되면 탭 판정을 끊고 높이 조절 모드로 들어간다.
    public void HandleHeaderBeginDrag()
    {
        if (!HasActiveSheetPage() || sheetRect == null || sheetPanel == null || !sheetPanel.activeSelf || IsHeaderGestureBlocked())
        {
            ResetHeaderGestureState();
            return;
        }

        CancelPendingSingleTap();
        waitingForSecondTap = false;
        lastHeaderTapTime = -10f;
        isDraggingHeader = true;
        suppressNextPointerUpTap = true;
    }

    // 드래그 중에는 손가락 이동량만큼 시트 높이를 반영한다.
    public void HandleHeaderDrag(float deltaY)
    {
        if (IsHeaderGestureBlocked())
        {
            ResetHeaderGestureState();
            return;
        }

        if (!isDraggingHeader || sheetRect == null)
            return;

        float nextHeight = sheetRect.sizeDelta.y + (deltaY / GetCanvasScaleFactor());
        ApplySheetHeight(nextHeight);
    }

    // 드래그 종료 시 닫힘, 기본, 최대 중 하나로 스냅한다.
    public void HandleHeaderEndDrag()
    {
        if (IsHeaderGestureBlocked())
        {
            ResetHeaderGestureState();
            return;
        }

        if (!isDraggingHeader)
            return;

        isDraggingHeader = false;
        waitingForSecondTap = false;
        lastHeaderTapTime = -10f;
        SnapSheetAfterDrag();
    }

    // 드래그 직후 들어오는 포인터 업 이벤트를 탭으로 처리하지 않도록 막는다.
    private bool ConsumeSuppressedPointerUp()
    {
        if (!suppressNextPointerUpTap)
            return false;

        suppressNextPointerUpTap = false;
        return true;
    }

    private void ResetHeaderGestureState()
    {
        CancelPendingSingleTap();
        waitingForSecondTap = false;
        isDraggingHeader = false;
        suppressNextPointerUpTap = false;
        lastHeaderTapTime = -10f;
    }

    private bool IsHeaderGestureBlocked()
    {
        if (HasActiveRaycastBlockingTarget(gestureBlockerContainer, false))
            return true;

        for (int i = 0; i < gestureBlockerRoots.Count; i++)
        {
            if (HasActiveRaycastBlockingTarget(gestureBlockerRoots[i], true))
                return true;
        }

        return false;
    }

    private static bool HasActiveRaycastBlockingTarget(Transform root, bool includeSelf)
    {
        if (root == null || !root.gameObject.activeInHierarchy)
            return false;

        if (includeSelf && IsRaycastBlockingTarget(root))
            return true;

        for (int i = 0; i < root.childCount; i++)
        {
            if (HasActiveRaycastBlockingTarget(root.GetChild(i), true))
                return true;
        }

        return false;
    }

    private static bool IsRaycastBlockingTarget(Transform target)
    {
        if (!AllowsRaycasts(target))
            return false;

        Graphic[] graphics = target.GetComponents<Graphic>();
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic != null && graphic.enabled && graphic.raycastTarget)
                return true;
        }

        return false;
    }

    private static bool AllowsRaycasts(Transform target)
    {
        Transform current = target;
        while (current != null)
        {
            CanvasGroup[] groups = current.GetComponents<CanvasGroup>();
            for (int i = 0; i < groups.Length; i++)
            {
                CanvasGroup group = groups[i];
                if (group == null || !group.enabled)
                    continue;

                if (!group.blocksRaycasts)
                    return false;

                if (group.ignoreParentGroups)
                    return true;
            }

            current = current.parent;
        }

        return true;
    }

    // 싱글 탭 닫기를 더블 탭 대기 시간만큼 미룬다.
    private void QueueSingleTapClose()
    {
        CancelPendingSingleTap();
        singleTapCoroutine = StartCoroutine(CloseAfterTapDelay());
    }

    // 일정 시간 안에 두 번째 탭이 없으면 시트를 닫는다.
    private IEnumerator CloseAfterTapDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, doubleTapThreshold));

        singleTapCoroutine = null;
        if (!waitingForSecondTap)
            yield break;

        waitingForSecondTap = false;
        lastHeaderTapTime = -10f;
        CloseActiveSheet();
    }

    // 예약된 싱글 탭 닫기 코루틴을 취소한다.
    private void CancelPendingSingleTap()
    {
        if (singleTapCoroutine == null)
            return;

        StopCoroutine(singleTapCoroutine);
        singleTapCoroutine = null;
    }

    // 드래그 종료 시 현재 높이를 기준으로 3단계 스냅을 판정한다.
    private void SnapSheetAfterDrag()
    {
        if (sheetRect == null)
            return;

        float currentHeight = sheetRect.sizeDelta.y;
        if (currentHeight <= GetCloseSnapThreshold())
        {
            CloseActiveSheet();
            return;
        }

        if (currentHeight >= GetExpandSnapThreshold())
        {
            ApplySheetHeight(GetMaxSheetHeight());
            return;
        }

        ResetSheetHeight();
    }

    // 캔버스 스케일에 맞춰 드래그 이동량을 보정한다.
    private float GetCanvasScaleFactor()
    {
        return parentCanvas != null && parentCanvas.scaleFactor > 0f ? parentCanvas.scaleFactor : 1f;
    }
#endregion

#region Layout
    // 시트 높이와 연결된 내부 레이아웃 높이를 함께 갱신한다.
    public void ApplySheetHeight(float height)
    {
        if (sheetRect == null)
            return;

        float clampedHeight = Mathf.Clamp(height, minSheetHeight, GetMaxSheetHeight());
        SetRectHeight(sheetRect, clampedHeight);
        SetLinkedHeight(contentsArea, clampedHeight - contentsAreaHeightOffset);
        SetLinkedHeight(contentsContainer, clampedHeight - contentsContainerHeightOffset);
        RefreshResizableContent();
    }

    // 기본 시트 높이는 실제 가능한 범위 안에서만 사용한다.
    private float GetRestSheetHeight()
    {
        return Mathf.Clamp(initialSheetHeight, minSheetHeight, GetMaxSheetHeight());
    }

    // RectTransform 높이 변경을 한 곳에서 처리한다.
    private static void SetRectHeight(RectTransform target, float height)
    {
        if (target == null)
            return;

        Vector2 sizeDelta = target.sizeDelta;
        float nextHeight = Mathf.Max(0f, height);
        if (Mathf.Approximately(sizeDelta.y, nextHeight))
            return;

        sizeDelta.y = nextHeight;
        target.sizeDelta = sizeDelta;
    }

    // 시트 높이에 종속되는 영역 높이를 계산해 반영한다.
    private static void SetLinkedHeight(RectTransform target, float height)
    {
        SetRectHeight(target, Mathf.Max(0f, height));
    }

    // 최대 높이는 override, TopPanel, 부모 높이 순으로 결정한다.
    public float GetMaxSheetHeight()
    {
        float maxHeight = 0f;
        if (maxSheetHeightOverride > 0f)
        {
            maxHeight = maxSheetHeightOverride;
        }
        else
        {
            maxHeight = GetMaxHeightByTopPanel();
            if (maxHeight <= 0f)
            {
                RectTransform parentRect = sheetRect != null ? sheetRect.parent as RectTransform : null;
                float parentHeight = parentRect != null ? parentRect.rect.height : 0f;

                if (parentHeight <= 0f && parentCanvas != null && parentCanvas.transform is RectTransform canvasRect)
                    parentHeight = canvasRect.rect.height;

                if (parentHeight > 0f)
                {
                    maxHeight = parentHeight;

                    if (maxSheetExtraHeight > 0f)
                        maxHeight = Mathf.Min(parentHeight, initialSheetHeight + maxSheetExtraHeight);
                }
                else
                {
                    maxHeight = maxSheetExtraHeight > 0f ? initialSheetHeight + maxSheetExtraHeight : initialSheetHeight;
                }
            }
        }

        return Mathf.Max(minSheetHeight, maxHeight);
    }

    // TopPanel 하단선까지 빈 공간 없이 맞닿도록 최대 높이를 계산한다.
    private float GetMaxHeightByTopPanel()
    {
        if (sheetRect == null || topPanelRect == null)
            return 0f;

        RectTransform referenceRect = parentCanvas != null ? parentCanvas.transform as RectTransform : sheetRect.parent as RectTransform;
        if (referenceRect == null)
            return 0f;

        float sheetBottom = GetBottomEdgeInReferenceSpace(sheetRect, referenceRect);
        float topPanelBottom = GetBottomEdgeInReferenceSpace(topPanelRect, referenceRect) - topPanelSpacing;
        return Mathf.Max(0f, topPanelBottom - sheetBottom);
    }

    // 대상 RectTransform의 하단선을 기준 좌표계로 변환한다.
    private static float GetBottomEdgeInReferenceSpace(RectTransform target, RectTransform referenceRect)
    {
        if (target == null || referenceRect == null)
            return 0f;

        Vector3 localBottom = new Vector3(0f, target.rect.yMin, 0f);
        Vector3 worldBottom = target.TransformPoint(localBottom);
        return referenceRect.InverseTransformPoint(worldBottom).y;
    }

    // 대상 RectTransform의 상단선을 기준 좌표계로 변환한다.
    private static float GetTopEdgeInReferenceSpace(RectTransform target, RectTransform referenceRect)
    {
        if (target == null || referenceRect == null)
            return 0f;

        Vector3 localTop = new Vector3(0f, target.rect.yMax, 0f);
        Vector3 worldTop = target.TransformPoint(localTop);
        return referenceRect.InverseTransformPoint(worldTop).y;
    }

    // 닫힘과 기본 높이의 중간 지점을 계산한다.
    private float GetCloseSnapThreshold()
    {
        float restHeight = GetRestSheetHeight();
        if (closeSnapHeight > 0f)
            return Mathf.Clamp(closeSnapHeight, minSheetHeight, restHeight);

        return (minSheetHeight + restHeight) * 0.5f;
    }

    // 기본과 최대 높이의 중간 지점을 계산한다.
    private float GetExpandSnapThreshold()
    {
        float restHeight = GetRestSheetHeight();
        float maxHeight = GetMaxSheetHeight();
        if (expandSnapHeight > 0f)
            return Mathf.Clamp(expandSnapHeight, restHeight, maxHeight);

        return (restHeight + maxHeight) * 0.5f;
    }

    // 시트를 기본 높이로 되돌린다.
    private void ResetSheetHeight()
    {
        if (sheetRect != null)
            ApplySheetHeight(GetRestSheetHeight());
    }

    // 현재 표시 중인 페이지와 내부 ScrollRect 높이를 시트에 맞춰 조절한다.
    private void RefreshResizableContent()
    {
        if (contentsArea == null)
            return;

        for (int i = 0; i < contentsArea.childCount; i++)
        {
            RectTransform page = contentsArea.GetChild(i) as RectTransform;
            if (page == null || !page.gameObject.activeSelf)
                continue;

            StretchToParent(page);

            PageResizeCache cache = GetOrCreatePageCache(page);
            ApplyPageResize(cache);
            RefreshScrollLayouts(cache);
            RebuildLayout(page);
        }

        RebuildLayout(contentsArea);
        Canvas.ForceUpdateCanvases();
    }

    // 페이지별 리사이즈 기준값을 캐시한다.
    private PageResizeCache GetOrCreatePageCache(RectTransform page)
    {
        for (int i = 0; i < pageResizeCaches.Count; i++)
        {
            if (pageResizeCaches[i].page == page)
                return pageResizeCaches[i];
        }

        PageResizeCache cache = new PageResizeCache
        {
            page = page
        };

        BottomPanelManagedPage managedPage = GetManagedPage(page);
        if (managedPage != null && !managedPage.UseLegacyScrollResize)
        {
            pageResizeCaches.Add(cache);
            return cache;
        }

        ScrollRect[] scrollRects = page.GetComponentsInChildren<ScrollRect>(false);
        for (int i = 0; i < scrollRects.Length; i++)
        {
            RectTransform rect = scrollRects[i].transform as RectTransform;
            if (rect == null)
                continue;

            ScrollRectResizeCache scrollCache = new ScrollRectResizeCache
            {
                rect = rect,
                isPageRoot = rect == page
            };

            if (!scrollCache.isPageRoot)
                CacheScrollRectInsets(page, scrollCache);

            cache.scrollRects.Add(scrollCache);
        }

        pageResizeCaches.Add(cache);
        return cache;
    }
    // 페이지 높이 변화량만큼 내부 ScrollRect 높이를 함께 늘리거나 줄인다.
    private void ApplyPageResize(PageResizeCache cache)
    {
        if (cache == null)
            return;

        for (int i = 0; i < cache.scrollRects.Count; i++)
        {
            ScrollRectResizeCache scrollCache = cache.scrollRects[i];
            if (scrollCache.rect == null)
                continue;

            if (scrollCache.isPageRoot)
                continue;

            ApplyScrollRectInsets(scrollCache);
        }
    }

    // ScrollRect의 초기 상하 여백을 캐시한다.
    private void CacheScrollRectInsets(RectTransform page, ScrollRectResizeCache scrollCache)
    {
        if (page == null || scrollCache == null || scrollCache.rect == null)
            return;

        RectTransform rect = scrollCache.rect;
        scrollCache.topInset = Mathf.Max(0f, -rect.offsetMax.y);
        scrollCache.bottomInset = Mathf.Max(0f, rect.offsetMin.y);
    }

    // ScrollRect는 원래 상하 여백을 유지한 채 세로 크기만 다시 맞춘다.
    private static void ApplyScrollRectInsets(ScrollRectResizeCache scrollCache)
    {
        RectTransform rect = scrollCache.rect;
        if (rect == null)
            return;

        Vector2 anchorMin = rect.anchorMin;
        anchorMin.y = 0f;
        rect.anchorMin = anchorMin;

        Vector2 anchorMax = rect.anchorMax;
        anchorMax.y = 1f;
        rect.anchorMax = anchorMax;

        Vector2 offsetMin = rect.offsetMin;
        offsetMin.y = scrollCache.bottomInset;
        rect.offsetMin = offsetMin;

        Vector2 offsetMax = rect.offsetMax;
        offsetMax.y = -scrollCache.topInset;
        rect.offsetMax = offsetMax;
    }

    // ScrollRect와 페이지 레이아웃을 다시 계산한다.
    private void RefreshScrollLayouts(PageResizeCache cache)
    {
        if (cache == null)
            return;

        for (int i = 0; i < cache.scrollRects.Count; i++)
        {
            ScrollRectResizeCache scrollCache = cache.scrollRects[i];
            if (scrollCache.rect != null)
                RebuildLayout(scrollCache.rect);
        }
    }

    // 즉시 레이아웃을 다시 빌드한다.
    private static void RebuildLayout(RectTransform rect)
    {
        if (rect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
    }

    // 세로 스트레치 상태인지 확인한다.
    private static bool IsVerticalStretch(RectTransform rect)
    {
        if (rect == null)
            return false;

        return Mathf.Approximately(rect.anchorMin.y, 0f) && Mathf.Approximately(rect.anchorMax.y, 1f);
    }

    // 부모 영역을 꽉 채우도록 RectTransform을 맞춘다.
    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
            return;

        RectTransform parent = rect.parent as RectTransform;
        if (parent == null)
            return;

        Vector3[] worldCorners = new Vector3[4];
        rect.GetWorldCorners(worldCorners);

        Vector3 bottomLeft = parent.InverseTransformPoint(worldCorners[0]);
        Vector3 topRight = parent.InverseTransformPoint(worldCorners[2]);
        Rect parentRect = parent.rect;

        float left = bottomLeft.x - parentRect.xMin;
        float bottom = bottomLeft.y - parentRect.yMin;
        float right = parentRect.xMax - topRight.x;
        float top = parentRect.yMax - topRight.y;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
#endregion

#region Utility
    // 활성 영역의 자식들을 원래 컨테이너로 되돌린다.
    private void OnExternalPageToggleChanged(ExternalPageToggleBinding binding, bool isOn)
    {
        if (binding == null || binding.page == null)
            return;

        if (isOn)
        {
            ShowManagedPage(binding.page);
            return;
        }

        if (IsManagedPageSelected(binding.page))
            CloseActiveSheet();
    }

    private void RefreshExternalPageToggles()
    {
        for (int i = 0; i < externalPageToggleBindings.Count; i++)
            SyncExternalPageToggle(externalPageToggleBindings[i]);
    }

    private void SyncExternalPageToggle(ExternalPageToggleBinding binding)
    {
        if (binding == null || binding.toggle == null)
            return;

        binding.toggle.SetIsOnWithoutNotify(IsManagedPageSelected(binding.page));
    }

    private bool IsManagedPageSelected(RectTransform page)
    {
        if (page == null)
            return false;

        if (TryFindManagedPage(page, out int tabIndex, out int pageIndex))
        {
            if (tabIndex != activeTab || !IsValidTabIndex(tabIndex))
                return false;

            TabConfig tab = tabs[tabIndex];
            if (!tab.routeBySubMenu)
                return true;

            if (activeSub < 0)
                return pageIndex == 0;

            return activeSub == pageIndex;
        }

        return TryResolveStandaloneManagedPage(page, out RectTransform standalonePage) &&
               activeTab < 0 &&
               activeStandalonePage == standalonePage;
    }

    private bool HasActiveSheetPage()
    {
        return activeTab >= 0 || activeStandalonePage != null;
    }

    private static RectTransform GetPage(TabConfig tab, int pageIndex)
    {
        if (tab == null || pageIndex < 0 || pageIndex >= tab.pages.Count)
            return null;

        return tab.pages[pageIndex];
    }

    private static BottomPanelManagedPage GetManagedPage(RectTransform page)
    {
        return page != null ? page.GetComponent<BottomPanelManagedPage>() : null;
    }

    private static void MoveChildren(RectTransform from, RectTransform to)
    {
        if (from == null || to == null)
            return;

        while (from.childCount > 0)
        {
            Transform child = from.GetChild(0);
            child.SetParent(to, false);
            child.gameObject.SetActive(false);
        }
    }

    // 대상 RectTransform을 지정한 부모에 붙이고 필요한 레이아웃을 적용한다.
    private static void AttachRect(RectTransform rectTransform, RectTransform target, bool stretchToParent)
    {
        if (rectTransform == null || target == null)
            return;

        rectTransform.SetParent(target, false);
        rectTransform.SetAsLastSibling();
        rectTransform.gameObject.SetActive(true);

        if (stretchToParent)
            StretchToParent(rectTransform);
    }

    // 현재 열려 있는 시트를 닫는 공용 진입점이다.
    private void CloseActiveSheet()
    {
        CancelPendingSingleTap();
        waitingForSecondTap = false;
        lastHeaderTapTime = -10f;
        suppressNextPointerUpTap = false;

        if (HasActiveSheetPage())
        {
            SelectMain(-1);
            return;
        }

        CloseSheet();
    }
#endregion
}
