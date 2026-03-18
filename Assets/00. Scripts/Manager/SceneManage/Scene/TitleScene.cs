using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScene : SceneBase
{
    [SerializeField] private SceneType nextScene = SceneType.StageScene;
    [SerializeField, Range(0.5f, 0.95f)] private float dataLoadProgressWeight = 0.9f;
    [SerializeField] private RectTransform loadingCanvasRoot;
    [SerializeField] private RectTransform loadingPanelRoot;
    [SerializeField] private Slider loadingSlider;
    [SerializeField] private TextMeshProUGUI loadingText;

    private DataManager dataManager;

    public override IEnumerator EnterScene()
    {
        dataManager = DataManager.Instance;
        ShowLoadingUi();
        SetLoadingProgress(0f);

        SubscribeLoadingEvents();

        if (!dataManager.DataLoad)
        {
            dataManager.LoadStart();
            yield return new WaitUntil(() => dataManager.DataLoad);
        }
        else
        {
            ApplyDataLoadProgress(1f);
        }

        UnsubscribeLoadingEvents();
        yield return StartCoroutine(LoadNextSceneAsync(nextScene));
    }

    public override void ExitScene()
    {
        UnsubscribeLoadingEvents();
    }

    private void HandleDataLoadProgress(float normalizedProgress, string currentFileName)
    {
        ApplyDataLoadProgress(normalizedProgress);
    }

    private void ApplyDataLoadProgress(float normalizedProgress)
    {
        SetLoadingProgress(Mathf.Clamp01(normalizedProgress) * dataLoadProgressWeight);
    }

    private IEnumerator LoadNextSceneAsync(SceneType sceneType)
    {
        if (UIRoot.Instance != null)
            UIRoot.Instance.PrepareForSceneTransfer();

        Scene sourceScene = gameObject.scene;
        string targetSceneName = sceneType.ToString();
        Scene loadedScene = default;
        bool targetSceneLoaded = false;

        void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!string.Equals(scene.name, targetSceneName, System.StringComparison.Ordinal))
                return;

            loadedScene = scene;
            targetSceneLoaded = true;
            SceneManager.SetActiveScene(scene);
        }

        SceneManager.sceneLoaded += HandleSceneLoaded;

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            yield break;
        }

        operation.allowSceneActivation = false;

        while (!operation.isDone || !targetSceneLoaded)
        {
            float sceneProgress = Mathf.Clamp01(operation.progress / 0.9f);
            float blendedProgress = Mathf.Lerp(dataLoadProgressWeight, 0.98f, sceneProgress);

            SetLoadingProgress(blendedProgress);

            if (sceneProgress >= 1f)
                operation.allowSceneActivation = true;

            yield return null;
        }

        SceneManager.sceneLoaded -= HandleSceneLoaded;

        StageScene stageScene = null;
        while (stageScene == null)
        {
            stageScene = FindSceneComponent<StageScene>(loadedScene);
            yield return null;
        }

        SetLoadingProgress(0.99f);
        yield return new WaitUntil(() => stageScene.IsSceneReady);
        SetLoadingProgress(1f);

        ExitScene();
        SceneManager.SetActiveScene(loadedScene);

        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sourceScene);
        if (unloadOperation == null)
            yield break;

        while (!unloadOperation.isDone)
            yield return null;
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

    private void SubscribeLoadingEvents()
    {
        if (dataManager == null)
            return;

        dataManager.OnNormalizedProgress -= HandleDataLoadProgress;
        dataManager.OnNormalizedProgress += HandleDataLoadProgress;
    }

    private void UnsubscribeLoadingEvents()
    {
        if (dataManager == null)
            return;

        dataManager.OnNormalizedProgress -= HandleDataLoadProgress;
    }

    private void ShowLoadingUi()
    {
        if (loadingCanvasRoot != null)
        {
            loadingCanvasRoot.gameObject.SetActive(true);

            if (loadingCanvasRoot.localScale == Vector3.zero)
                loadingCanvasRoot.localScale = Vector3.one;
        }

        if (loadingPanelRoot != null)
            loadingPanelRoot.gameObject.SetActive(true);
    }

    private void SetLoadingProgress(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        ShowLoadingUi();

        if (loadingSlider != null)
        {
            loadingSlider.minValue = 0f;
            loadingSlider.maxValue = 1f;
            loadingSlider.value = clampedProgress;
        }

        if (loadingText != null)
            loadingText.text = $"{(clampedProgress * 100f):F0}%";
    }
}
