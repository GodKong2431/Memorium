using UnityEngine;
using UnityEngine.SceneManagement;

public class UIRoot : Singleton<UIRoot>
{
    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BroadcastMessage("RefreshView", SendMessageOptions.DontRequireReceiver);
    }
}