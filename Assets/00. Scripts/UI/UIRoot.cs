using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIRoot : MonoBehaviour
{
    public static UIRoot Instance { get; private set; }

    private Coroutine sceneRefreshRoutine;
    private bool isTransferringBetweenScenes;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            DisableDuplicateRoot();
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (sceneRefreshRoutine != null)
        {
            StopCoroutine(sceneRefreshRoutine);
            sceneRefreshRoutine = null;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void PrepareForSceneTransfer()
    {
        if (isTransferringBetweenScenes)
            return;

        isTransferringBetweenScenes = true;
        DontDestroyOnLoad(gameObject);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (sceneRefreshRoutine != null)
            StopCoroutine(sceneRefreshRoutine);

        if (isTransferringBetweenScenes && scene.IsValid())
        {
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            isTransferringBetweenScenes = false;
        }

        ScenePlayerLocator.ClearCachedPlayerTransform();
        BroadcastMessage("ResetForSceneChange", SendMessageOptions.DontRequireReceiver);
        sceneRefreshRoutine = StartCoroutine(RefreshSceneUiRoutine());
    }

    private IEnumerator RefreshSceneUiRoutine()
    {
        yield return null;

        BroadcastMessage("RefreshView", SendMessageOptions.DontRequireReceiver);
        sceneRefreshRoutine = null;
    }

    private void DisableDuplicateRoot()
    {
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
