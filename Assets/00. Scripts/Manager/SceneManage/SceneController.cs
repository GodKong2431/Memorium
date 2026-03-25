using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    [Header("Scene Transition")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField] private Color transitionColor = Color.black;
    [SerializeField] private bool useRadialIris = true;
    [SerializeField, Range(0.01f, 1f)] private float irisSoftness = 0.18f;
    [SerializeField] private int overlaySortingOrder = 32000;

    private bool isLoading;
    private SceneTransitionOverlay transitionOverlay;
    private Coroutine externalTransitionRoutine;

    public bool IsLoading => isLoading;

    public void LoadScene(SceneType type)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(type));
    }

    public void FinalizeExternalTransition(Scene sourceScene)
    {
        if (externalTransitionRoutine != null)
            StopCoroutine(externalTransitionRoutine);

        externalTransitionRoutine = StartCoroutine(FinalizeExternalTransitionRoutine(sourceScene));
    }

    public IEnumerator FadeOutTransition()
    {
        SceneTransitionOverlay overlay = GetOrCreateOverlay();
        if (overlay == null)
            yield break;

        overlay.Configure(transitionColor, useRadialIris, irisSoftness, overlaySortingOrder);
        yield return overlay.FadeOut(fadeOutDuration);
    }

    public IEnumerator FadeInTransition()
    {
        SceneTransitionOverlay overlay = GetOrCreateOverlay();
        if (overlay == null)
            yield break;

        overlay.Configure(transitionColor, useRadialIris, irisSoftness, overlaySortingOrder);
        yield return overlay.FadeIn(fadeInDuration);
    }

    private IEnumerator LoadSceneRoutine(SceneType type)
    {
        isLoading = true;
        string targetSceneName = type.ToString();

        SceneBase currentScene = Object.FindFirstObjectByType<SceneBase>();
        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        if (operation == null)
        {
            isLoading = false;
            yield break;
        }

        operation.allowSceneActivation = false;

        yield return FadeOutTransition();

        if (currentScene != null)
            currentScene.ExitScene();

        while (operation.progress < 0.9f)
            yield return null;

        operation.allowSceneActivation = true;

        while (!operation.isDone)
            yield return null;

        yield return WaitForSceneReady(targetSceneName);
        yield return null;
        yield return new WaitForEndOfFrame();
        yield return FadeInTransition();

        isLoading = false;
    }

    private IEnumerator WaitForSceneReady(string targetSceneName)
    {
        Scene targetScene = default;
        while (!targetScene.IsValid() || !targetScene.isLoaded)
        {
            targetScene = SceneManager.GetSceneByName(targetSceneName);
            yield return null;
        }

        SceneBase sceneBase = null;
        int waitFrames = 0;
        while (sceneBase == null && waitFrames < 120)
        {
            sceneBase = FindSceneComponent<SceneBase>(targetScene);
            if (sceneBase != null)
                break;

            waitFrames++;
            yield return null;
        }

        if (sceneBase == null)
            yield break;

        while (!sceneBase.IsSceneReady)
            yield return null;
    }

    private SceneTransitionOverlay GetOrCreateOverlay()
    {
        if (transitionOverlay != null)
            return transitionOverlay;

        transitionOverlay = GetComponent<SceneTransitionOverlay>();
        if (transitionOverlay == null)
            transitionOverlay = gameObject.AddComponent<SceneTransitionOverlay>();

        return transitionOverlay;
    }

    private IEnumerator FinalizeExternalTransitionRoutine(Scene sourceScene)
    {
        if (sourceScene.IsValid() && sourceScene.isLoaded)
        {
            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sourceScene);
            if (unloadOperation != null)
            {
                while (!unloadOperation.isDone)
                    yield return null;
            }
        }

        yield return null;
        yield return new WaitForEndOfFrame();
        yield return FadeInTransition();

        externalTransitionRoutine = null;
        isLoading = false;
    }

    private static T FindSceneComponent<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] rootObjects = scene.GetRootGameObjects();
        for (int i = 0; i < rootObjects.Length; i++)
        {
            T component = rootObjects[i].GetComponentInChildren<T>(true);
            if (component != null)
                return component;
        }

        return null;
    }
}
