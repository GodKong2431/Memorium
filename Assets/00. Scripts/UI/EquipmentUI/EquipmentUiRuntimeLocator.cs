using TMPro;
using UnityEngine;
using UnityEngine.UI;

internal static class EquipmentUiRuntimeLocator
{
    public static RectTransform FindRectTransform(string objectName, Transform searchRoot = null)
    {
        Transform target = FindChild(objectName, searchRoot);
        return target as RectTransform;
    }

    public static Button FindButton(string objectName, Transform searchRoot = null)
    {
        return FindComponent<Button>(objectName, searchRoot);
    }

    public static TextMeshProUGUI FindText(string objectName, Transform searchRoot = null)
    {
        return FindComponent<TextMeshProUGUI>(objectName, searchRoot);
    }

    public static Image FindImage(string objectName, Transform searchRoot = null)
    {
        return FindComponent<Image>(objectName, searchRoot);
    }

    public static T FindComponent<T>(string objectName, Transform searchRoot = null) where T : Component
    {
        Transform root = ResolveSearchRoot(searchRoot);
        return FindRecursiveComponent<T>(root, objectName);
    }

    public static Transform FindChild(string objectName, Transform searchRoot = null)
    {
        Transform root = ResolveSearchRoot(searchRoot);
        return FindRecursive(root, objectName);
    }

    private static Transform ResolveSearchRoot(Transform searchRoot)
    {
        if (searchRoot != null)
            return searchRoot;

        if (UIRoot.Instance != null)
            return UIRoot.Instance.transform;

        UIRoot runtimeRoot = Object.FindFirstObjectByType<UIRoot>();
        return runtimeRoot != null ? runtimeRoot.transform : null;
    }

    private static Transform FindRecursive(Transform current, string objectName)
    {
        if (current == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        if (current.name == objectName)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform found = FindRecursive(current.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }

    private static T FindRecursiveComponent<T>(Transform current, string objectName) where T : Component
    {
        if (current == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        if (current.name == objectName)
        {
            T component = current.GetComponent<T>();
            if (component != null)
                return component;
        }

        for (int i = 0; i < current.childCount; i++)
        {
            T found = FindRecursiveComponent<T>(current.GetChild(i), objectName);
            if (found != null)
                return found;
        }

        return null;
    }
}
