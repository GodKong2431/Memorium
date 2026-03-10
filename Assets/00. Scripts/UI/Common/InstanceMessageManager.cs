using System.Collections;
using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class InstanceMessageManager : MonoBehaviour
{
    private const string ManagerHostObjectName = "PopupPanel";
    private const string PanelObjectName = "(Panel)InstanceMasage";
    private const string BossEnterPanelObjectName = "(Panel)BossEnter";
    private const string DefaultTextObjectName = "Text (TMP)";
    private const float DefaultDuration = 1.2f;
    private const float BossEnterDuration = 1.5f;
    private const string DungeonInProgressMessage = "\uC774\uBBF8 \uB358\uC804\uC744 \uC9C4\uD589 \uC911\uC785\uB2C8\uB2E4.";
    private const string InsufficientGoldMessage = "\uB3C8\uC774 \uBD80\uC871\uD569\uB2C8\uB2E4.";

    private static InstanceMessageManager instance;

    private GameObject instanceMessagePanelObject;
    private GameObject bossEnterPanelObject;
    private TextMeshProUGUI messageText;
    private Coroutine instanceMessageHideCoroutine;
    private Coroutine bossEnterHideCoroutine;

    public static bool TryShow(string message, float duration = DefaultDuration)
    {
        InstanceMessageManager manager = ResolveInstance();
        if (manager == null)
        {
            Debug.LogWarning("[InstanceMessageManager] '(Panel)InstanceMasage' object was not found in the active scene.");
            return false;
        }

        return manager.ShowInternal(message, duration);
    }

    public static bool TryShowDungeonInProgress(float duration = DefaultDuration)
    {
        return TryShow(DungeonInProgressMessage, duration);
    }

    public static bool TryShowInsufficientGold(float duration = DefaultDuration)
    {
        return TryShow(InsufficientGoldMessage, duration);
    }

    public static bool TryShowBossEnter(float duration = BossEnterDuration)
    {
        InstanceMessageManager manager = ResolveInstance();
        if (manager == null)
        {
            Debug.LogWarning("[InstanceMessageManager] 'PopupPanel' object was not found in the active scene.");
            return false;
        }

        return manager.ShowBossEnterInternal(duration);
    }

    private static InstanceMessageManager ResolveInstance()
    {
        if (instance != null)
            return instance;

        GameObject hostObject = FindSceneObjectByName(ManagerHostObjectName);
        if (hostObject == null)
            hostObject = FindSceneObjectByName(PanelObjectName);

        if (hostObject == null)
            return null;

        instance = hostObject.GetComponent<InstanceMessageManager>();
        if (instance == null)
            instance = hostObject.AddComponent<InstanceMessageManager>();

        instance.CacheReferences();
        return instance;
    }

    private static GameObject FindSceneObjectByName(string objectName)
    {
        GameObject[] objects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < objects.Length; i++)
        {
            GameObject candidate = objects[i];
            if (candidate == null)
                continue;

            if (candidate.name != objectName)
                continue;

            if (!candidate.scene.IsValid())
                continue;

            return candidate;
        }

        return null;
    }

    private bool ShowInternal(string message, float duration)
    {
        if (instanceMessageHideCoroutine != null)
            return false;

        CacheReferences();

        if (messageText != null && !string.IsNullOrEmpty(message))
            messageText.text = message;

        if (instanceMessagePanelObject == null)
            return false;

        instanceMessagePanelObject.SetActive(true);
        instanceMessageHideCoroutine = StartCoroutine(HideInstanceMessageAfterDelay(Mathf.Max(0.1f, duration)));
        return true;
    }

    private bool ShowBossEnterInternal(float duration)
    {
        if (bossEnterHideCoroutine != null)
            return false;

        CacheReferences();
        if (bossEnterPanelObject == null)
            return false;

        bossEnterPanelObject.SetActive(true);
        bossEnterHideCoroutine = StartCoroutine(HideBossEnterAfterDelay(Mathf.Max(0.1f, duration)));
        return true;
    }

    private IEnumerator HideInstanceMessageAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        instanceMessageHideCoroutine = null;
        if (instanceMessagePanelObject != null)
            instanceMessagePanelObject.SetActive(false);
    }

    private IEnumerator HideBossEnterAfterDelay(float duration)
    {
        yield return new WaitForSecondsRealtime(duration);

        bossEnterHideCoroutine = null;
        if (bossEnterPanelObject != null)
            bossEnterPanelObject.SetActive(false);
    }

    private void CacheReferences()
    {
        if (instanceMessagePanelObject == null)
            instanceMessagePanelObject = FindChildObject(PanelObjectName);

        if (bossEnterPanelObject == null)
            bossEnterPanelObject = FindChildObject(BossEnterPanelObjectName);

        if (messageText == null && instanceMessagePanelObject != null)
        {
            TextMeshProUGUI[] texts = instanceMessagePanelObject.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null)
                    continue;

                if (texts[i].gameObject.name == DefaultTextObjectName)
                {
                    messageText = texts[i];
                    return;
                }
            }

            if (texts.Length > 0)
                messageText = texts[0];
        }
    }

    private GameObject FindChildObject(string objectName)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i] == null)
                continue;

            if (children[i].name == objectName)
                return children[i].gameObject;
        }

        return FindSceneObjectByName(objectName);
    }

    private void OnDisable()
    {
        if (instanceMessageHideCoroutine != null)
        {
            StopCoroutine(instanceMessageHideCoroutine);
            instanceMessageHideCoroutine = null;
        }

        if (bossEnterHideCoroutine != null)
        {
            StopCoroutine(bossEnterHideCoroutine);
            bossEnterHideCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
