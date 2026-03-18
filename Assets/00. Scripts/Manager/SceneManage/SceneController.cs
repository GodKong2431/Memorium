using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    private bool isLoading;

    public void LoadScene(SceneType type)
    {
        if (isLoading)
            return;

        StartCoroutine(LoadSceneRoutine(type));
    }

    private IEnumerator LoadSceneRoutine(SceneType type)
    {
        isLoading = true;
        string targetSceneName = type.ToString();

        if (UIRoot.Instance != null)
            UIRoot.Instance.PrepareForSceneTransfer();

        SceneBase currentScene = Object.FindFirstObjectByType<SceneBase>();
        if (currentScene != null)
            currentScene.ExitScene();

        AsyncOperation operation = SceneManager.LoadSceneAsync(targetSceneName);
        operation.allowSceneActivation = false;

        float timer = 0f;

        while (!operation.isDone)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            if (progress >= 1f && timer >= 1.0f)
                operation.allowSceneActivation = true;

            yield return null;
        }

        isLoading = false;
    }
}
