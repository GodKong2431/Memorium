using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : Singleton<SceneController>
{
    [Header("로딩 UI 설정")]
    public LoadingUI loadingUI;

    private bool _isLoading;

    public void LoadScene(SceneType type)
    {
        if (_isLoading) return;
        StartCoroutine(LoadSceneRoutine(type));
    }

    private IEnumerator LoadSceneRoutine(SceneType type)
    {
        _isLoading = true;
        string targetSceneName = type.ToString();

        SceneBase currentScene = Object.FindFirstObjectByType<SceneBase>();
        if (currentScene != null)
        {
            currentScene.ExitScene();
        }

        if (loadingUI != null)
        {
            loadingUI.gameObject.SetActive(true);
            loadingUI.SetProgress(0f);
        }

        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneName);
        op.allowSceneActivation = false;

        float timer = 0f;

        while (!op.isDone)
        {
            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(op.progress / 0.9f);

            if (progress >= 1f && timer >= 1.0f)
            {
                if (loadingUI != null) loadingUI.SetProgress(1f);
                op.allowSceneActivation = true;
            }
            else
            {
                if (loadingUI != null) loadingUI.SetProgress(progress);
            }

            yield return null;
        }

        if (loadingUI != null)
        {
            loadingUI.gameObject.SetActive(false);
        }

        _isLoading = false;
    }
}