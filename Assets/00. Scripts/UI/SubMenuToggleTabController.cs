using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SubMenuToggleTabController : MonoBehaviour
{
    // 탭 1개와 연결된 콘텐츠/그래픽 설정.
    [Serializable]
    private class TabLink
    {
        [SerializeField] public Toggle toggle; // 탭 토글.
        [SerializeField] public GameObject content; // 탭 선택 시 활성화할 콘텐츠 루트.
        [SerializeField] public Graphic visual; // 탭 선택/비선택 색상을 적용할 그래픽.

        [NonSerialized] public Color baseColor; // 비선택 색 계산을 위한 원본 색상.
        [NonSerialized] public bool hasBaseColor; // 원본 색상 캐시 여부.
    }

    [SerializeField] private List<TabLink> tabLinks = new List<TabLink>(); // 탭-콘텐츠 매핑 목록.
    [SerializeField] private bool selectFirst = true; // 활성화 시 첫 탭 자동 선택 여부.
    [SerializeField] [Range(0f, 1f)] private float dim = 0.65f; // 비선택 탭 밝기 배율.

    private int activeIndex = -1; // 현재 선택된 탭 인덱스.

    // 탭 이벤트와 기본 그래픽 캐시를 초기화한다.
    private void Awake()
    {
        for (int i = 0; i < tabLinks.Count; i++)
            Bind(i);
    }

    // 컨트롤러 활성화 시 초기 탭을 선택한다.
    private void OnEnable()
    {
        int startIndex = GetStartIndex();
        if (startIndex < 0)
            return;

        Select(startIndex);
    }

    // 지정한 인덱스 탭을 선택하고 콘텐츠/시각 상태를 갱신한다.
    public void Select(int index)
    {
        if (!IsValid(index))
            return;

        activeIndex = index;
        float brightness = Mathf.Clamp01(dim);

        for (int i = 0; i < tabLinks.Count; i++)
        {
            TabLink tab = tabLinks[i];
            if (tab == null || tab.toggle == null)
                continue;

            bool isSelected = i == activeIndex;
            tab.toggle.SetIsOnWithoutNotify(isSelected);

            if (tab.content != null)
                tab.content.SetActive(isSelected);

            UpdateVisual(tab, isSelected, brightness);
        }
    }

    // 현재 선택된 탭 인덱스를 반환한다.
    public int GetSelectedIndex()
    {
        return activeIndex;
    }

    // 개별 탭의 토글 이벤트와 그래픽 캐시를 연결한다.
    private void Bind(int index)
    {
        TabLink tab = tabLinks[index];
        if (tab == null || tab.toggle == null)
            return;

        Toggle toggle = tab.toggle;

        if (tab.visual != null && !tab.hasBaseColor)
        {
            tab.baseColor = tab.visual.color;
            tab.hasBaseColor = true;
        }

        if (toggle.graphic == tab.visual)
            toggle.graphic = null;

        int captured = index;
        toggle.onValueChanged.AddListener(isOn => OnToggleChanged(captured, isOn));
    }

    // 토글 On/Off 변화에 따라 단일 선택 상태를 유지한다.
    private void OnToggleChanged(int index, bool isOn)
    {
        if (isOn)
        {
            Select(index);
            return;
        }

        if (index == activeIndex && IsValid(index))
            tabLinks[index].toggle.SetIsOnWithoutNotify(true);
    }

    // 시작 시 선택할 인덱스를 계산한다.
    private int GetStartIndex()
    {
        int firstValid = FindFirstValid();
        if (firstValid < 0)
            return -1;

        if (selectFirst)
            return firstValid;

        int onIndex = FindCurrentOn();
        return onIndex >= 0 ? onIndex : firstValid;
    }

    // 유효한 탭 중 첫 번째 인덱스를 찾는다.
    private int FindFirstValid()
    {
        for (int i = 0; i < tabLinks.Count; i++)
        {
            if (IsValid(i))
                return i;
        }

        return -1;
    }

    // 현재 On 상태인 탭 인덱스를 찾는다.
    private int FindCurrentOn()
    {
        for (int i = 0; i < tabLinks.Count; i++)
        {
            if (IsValid(i) && tabLinks[i].toggle.isOn)
                return i;
        }

        return -1;
    }

    // 인덱스가 범위 내이며 토글이 연결된 유효 탭인지 검사한다.
    private bool IsValid(int index)
    {
        if (index < 0 || index >= tabLinks.Count)
            return false;

        TabLink tab = tabLinks[index];
        return tab != null && tab.toggle != null;
    }

    // 탭 선택 상태에 따라 그래픽 색상과 알파를 갱신한다.
    private static void UpdateVisual(TabLink tab, bool isSelected, float brightness)
    {
        if (tab == null || tab.visual == null)
            return;

        if (!tab.hasBaseColor)
        {
            tab.baseColor = tab.visual.color;
            tab.hasBaseColor = true;
        }

        Color baseColor = tab.baseColor;
        Color nextColor = isSelected
            ? baseColor
            : new Color(baseColor.r * brightness, baseColor.g * brightness, baseColor.b * brightness, baseColor.a);

        tab.visual.color = nextColor;
        if (tab.visual.canvasRenderer != null)
            tab.visual.canvasRenderer.SetAlpha(nextColor.a > 0f ? nextColor.a : 1f);
    }
}
