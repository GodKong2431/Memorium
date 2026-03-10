using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 장비 아이템 셀 하나의 렌더링과 상호작용 상태를 제어한다.
public sealed class EquipItemView
{
    // 숨김 패널 처리에 사용하는 투명 색상이다.
    private static readonly Color Transparent = new Color(1f, 1f, 1f, 0f);
    // 잠금(디밍) 상태에서 적용할 알파 값이다.
    private const float DimAlpha = 0.35f;

    // 이 셀의 UI 바인딩 참조다.
    private readonly EquipItemUI ui;
    // 런타임에 관리하는 별 이미지 목록이다.
    private readonly List<Image> stars = new List<Image>();

    // 디밍 표현에 사용하는 CanvasGroup 캐시다.
    private CanvasGroup canvasGroup;

    // UI 바인딩을 받아 뷰 래퍼를 초기화한다.
    public EquipItemView(EquipItemUI ui)
    {
        this.ui = ui;
        stars.Add(ui.TierStar);
    }

    // 아이템 버튼 클릭 콜백을 바인딩한다.
    public void Bind(UnityAction onClick)
    {
        ui.Button.onClick.RemoveAllListeners();
        ui.Button.onClick.AddListener(onClick);
    }

    // 아이콘, 레벨 텍스트, 티어 별 UI를 렌더링한다.
    public void Render(Sprite icon, string levelText, int starCount, Color tierColor)
    {
        ui.Icon.sprite = icon;
        ui.LevelText.text = levelText;
        ui.TierPanel.color = Transparent;

        SyncStars(Mathf.Max(1, starCount), tierColor);
    }
    // 아이콘, 레벨 텍스트, 티어 별 UI를 렌더링한다.
    public void RenderLevel (string levelText)
    {
        ui.LevelText.text = levelText;
    }

    // 보유 개수와 합성 요구 개수 슬라이더를 렌더링한다.
    public void RenderCount(int count, int mergeCount)
    {
        int required = Mathf.Max(1, mergeCount);
        int owned = Mathf.Max(0, count);

        ui.MergeSlider.minValue = 0f;
        ui.MergeSlider.maxValue = required;
        ui.MergeSlider.SetValueWithoutNotify(Mathf.Clamp(owned, 0, required));
        ui.CurrentCountText.text = owned > 999 ? "999+" : owned.ToString();
        ui.NeedCountText.text = required.ToString();
    }

    // 프레임 이미지 전체에 동일한 색상을 적용한다.
    public void SetFrameColor(Color color)
    {
        Image[] frames = ui.Frames;
        for (int i = 0; i < frames.Length; i++)
            frames[i].color = color;
    }

    // 잠금 여부에 따라 클릭 가능 상태와 알파를 반영한다.
    public void SetDimmed(bool dimmed)
    {
        ui.Button.interactable = !dimmed;

        if (canvasGroup == null)
        {
            canvasGroup = ui.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = ui.gameObject.AddComponent<CanvasGroup>();
        }

        if (canvasGroup == null)
            return;

        canvasGroup.alpha = dimmed ? DimAlpha : 1f;
    }

    // 필요한 별 개수를 맞추고 활성/색상을 갱신한다.
    private void SyncStars(int required, Color color)
    {
        while (stars.Count < required)
        {
            Image clone = Object.Instantiate(stars[0], ui.TierRoot);
            clone.name = $"(Img)TierStar_{stars.Count + 1}";
            stars.Add(clone);
        }

        for (int i = 0; i < stars.Count; i++)
        {
            bool active = i < required;
            stars[i].gameObject.SetActive(active);
            if (active)
                stars[i].color = color;
        }
    }
}
