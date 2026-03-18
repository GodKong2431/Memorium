using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _applicationIsQuitting = false;

    public static T Instance
    {
        get
        {
            if (_applicationIsQuitting)
            {
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)Object.FindFirstObjectByType(typeof(T));
                    if (Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length == 1)
                    {
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                    else if (Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length > 1)
                    {
                        Debug.LogWarning($"[Singleton] {typeof(T)}가 씬에 {Object.FindObjectsByType<T>(FindObjectsSortMode.None).Length}개 이상 존재합니다.");
                        return _instance;
                    }

                    if (_instance == null)
                    {
                        GameObject singletonObject = new GameObject();
                        _instance = singletonObject.AddComponent<T>();
                        singletonObject.name = typeof(T).ToString() + " (Singleton)";

                        DontDestroyOnLoad(singletonObject);
                    }


                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        if (_instance == this)
        {
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}

public sealed class DeferredAddressablesRelease : MonoBehaviour
{
    private struct PendingRelease
    {
        public AsyncOperationHandle handle;
        public int releaseFrame;
    }

    private static DeferredAddressablesRelease instance;
    private static bool applicationIsQuitting;
    private readonly List<PendingRelease> pendingReleases = new List<PendingRelease>();

    public static void Release<TObject>(AsyncOperationHandle<TObject> handle)
    {
        if (!handle.IsValid() || applicationIsQuitting)
            return;

        AsyncOperationHandle typelessHandle = handle;
        EnsureInstance().pendingReleases.Add(new PendingRelease
        {
            handle = typelessHandle,
            releaseFrame = Time.frameCount + 1
        });
    }

    public static void Release(AsyncOperationHandle handle)
    {
        if (!handle.IsValid() || applicationIsQuitting)
            return;

        EnsureInstance().pendingReleases.Add(new PendingRelease
        {
            handle = handle,
            releaseFrame = Time.frameCount + 1
        });
    }

    private static DeferredAddressablesRelease EnsureInstance()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<DeferredAddressablesRelease>();
        if (instance != null)
            return instance;

        GameObject root = new GameObject(nameof(DeferredAddressablesRelease));
        DontDestroyOnLoad(root);
        instance = root.AddComponent<DeferredAddressablesRelease>();
        return instance;
    }

    private void LateUpdate()
    {
        for (int i = pendingReleases.Count - 1; i >= 0; i--)
        {
            PendingRelease pendingRelease = pendingReleases[i];
            if (Time.frameCount < pendingRelease.releaseFrame)
                continue;

            if (pendingRelease.handle.IsValid())
                Addressables.Release(pendingRelease.handle);

            pendingReleases.RemoveAt(i);
        }
    }

    private void OnApplicationQuit()
    {
        applicationIsQuitting = true;
        pendingReleases.Clear();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }
}
