using System;
using TMPro;
using UnityEngine;

public class AdditionalContentController : MonoBehaviour
{
    [Header("Roots")]
    [SerializeField] private RectTransform contentsContainer;
    [SerializeField] private RectTransform contentsArea;

    private const string ScrollViewPrefix = "\uC2A4\uD06C\uB864\uBDF0_";
    private RectTransform currentContent;
    private RectTransform rectTransform;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OpenContent(RectTransform content)
    {
        currentContent = rectTransform;

        if (currentContent != null && currentContent != content)
        {
            MoveContent(currentContent, contentsContainer);
            currentContent.gameObject.SetActive(false);
        }

        MoveContent(content, contentsArea);
        content.gameObject.SetActive(true);

        currentContent = content;
    }

    private void MoveContent(RectTransform content, RectTransform targetParent)
    {
        if (content == null || targetParent == null)
            return;

        if (content.parent != targetParent)
            content.SetParent(targetParent, false);
    }

    private void ApplyVisibility(GameObject target, bool isSelected)
    {

            target.SetActive(isSelected);
    }
}

