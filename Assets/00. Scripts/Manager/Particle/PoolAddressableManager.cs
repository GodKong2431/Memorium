using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
public class PoolAddressableManager : Singleton<PoolAddressableManager>
{
    private const string PrefabRootPath = "Assets/02. Prefabs/";
    private const string PrefabsLabel = "Prefabs";

    private readonly Dictionary<string, GameObject> prefabCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, AsyncOperationHandle<GameObject>> prefabHandleCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> aliasToCanonicalKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<Action<GameObject>>> pendingCallbacks = new(StringComparer.Ordinal);
    private readonly HashSet<string> pendingLoads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<IResourceLocation>> prefabLocationsByFileName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action> pendingPrefabLocationCacheReady = new();

    private AsyncOperationHandle<IList<IResourceLocation>> prefabLocationsHandle;
    private bool prefabLocationCacheLoaded;
    private bool prefabLocationCacheLoading;

    public void Preload(string key)
    {
        if (!IsValidKey(key) || TryGetCachedObject(key, out _))
            return;

        QueueLoad(key, null);
    }


    /// <summary>
    /// Preload 했을 경우
    /// </summary>
    public GameObject GetPooledObject(string key, Vector3 position, Quaternion rotation)
    {
        if (!TryGetCachedObject(key, out GameObject prefab))
            return null;

        return ObjectPoolManager.Get(prefab, position, rotation);
    }

    /// <summary>
    /// Preload 안 했을경우 콜백과 함께 요청
    /// </summary>
    public void GetPooledObject(string key, Vector3 position, Quaternion rotation,
                                 System.Action<GameObject> onLoaded)
    {
        if (TryGetCachedObject(key, out GameObject prefab))
        {
            onLoaded?.Invoke(ObjectPoolManager.Get(prefab, position, rotation));
            return;
        }

        QueueLoad(key, loadedPrefab =>
        {
            if (loadedPrefab == null)
            {
                onLoaded?.Invoke(null);
                return;
            }

            onLoaded?.Invoke(ObjectPoolManager.Get(loadedPrefab, position, rotation));
        });
    }

    private void QueueLoad(string key, Action<GameObject> onLoaded)
    {
        string normalizedKey = NormalizeKey(key);
        List<string> candidateKeys = BuildCandidateKeys(key);
        string requestKey = GetPrimaryAlias(key);

        if (TryGetCachedObject(candidateKeys, out GameObject cachedPrefab))
        {
            onLoaded?.Invoke(cachedPrefab);
            return;
        }

        if (onLoaded != null)
        {
            if (!pendingCallbacks.TryGetValue(requestKey, out List<Action<GameObject>> callbacks))
            {
                callbacks = new List<Action<GameObject>>();
                pendingCallbacks[requestKey] = callbacks;
            }

            callbacks.Add(onLoaded);
        }

        if (!pendingLoads.Add(requestKey))
            return;

        ResolveAndLoad(requestKey, normalizedKey, candidateKeys, 0);
    }

    private void ResolveAndLoad(string requestKey, string originalKey, IReadOnlyList<string> candidateKeys, int index)
    {
        if (index >= candidateKeys.Count)
        {
            ResolveFromPrefabsLabel(requestKey, originalKey, candidateKeys);
            return;
        }

        string candidateKey = candidateKeys[index];
        AsyncOperationHandle<IList<IResourceLocation>> locationHandle =
            Addressables.LoadResourceLocationsAsync(candidateKey, typeof(GameObject));

        locationHandle.Completed += locationsOperation =>
        {
            bool hasLocation = locationsOperation.Status == AsyncOperationStatus.Succeeded
                && locationsOperation.Result != null
                && locationsOperation.Result.Count > 0;

            if (!hasLocation)
            {
                DeferredAddressablesRelease.Release(locationsOperation);
                ResolveAndLoad(requestKey, originalKey, candidateKeys, index + 1);
                return;
            }

            LoadResolvedLocation(
                requestKey,
                candidateKeys,
                locationsOperation.Result[0],
                () => DeferredAddressablesRelease.Release(locationsOperation),
                () =>
                {
                    DeferredAddressablesRelease.Release(locationsOperation);
                    ResolveAndLoad(requestKey, originalKey, candidateKeys, index + 1);
                });
        };
    }

    private void ResolveFromPrefabsLabel(string requestKey, string originalKey, IReadOnlyList<string> candidateKeys)
    {
        EnsurePrefabLocationCacheLoaded(() =>
        {
            IResourceLocation location = FindBestPrefabLocation(originalKey, candidateKeys);
            if (location == null)
            {
                FinishLoad(requestKey, null);
                return;
            }

            LoadResolvedLocation(requestKey, candidateKeys, location, null, () => FinishLoad(requestKey, null));
        });
    }

    private void EnsurePrefabLocationCacheLoaded(Action onReady)
    {
        if (prefabLocationCacheLoaded)
        {
            onReady?.Invoke();
            return;
        }

        if (onReady != null)
        {
            pendingPrefabLocationCacheReady.Add(onReady);
        }

        if (prefabLocationCacheLoading)
            return;

        prefabLocationCacheLoading = true;
        prefabLocationsHandle = Addressables.LoadResourceLocationsAsync(PrefabsLabel, typeof(GameObject));
        prefabLocationsHandle.Completed += operation =>
        {
            prefabLocationCacheLoading = false;
            prefabLocationCacheLoaded = operation.Status == AsyncOperationStatus.Succeeded && operation.Result != null;

            prefabLocationsByFileName.Clear();
            if (prefabLocationCacheLoaded)
            {
                for (int i = 0; i < operation.Result.Count; i++)
                {
                    IResourceLocation location = operation.Result[i];
                    string fileName = Path.GetFileName(NormalizeKey(location.PrimaryKey));
                    if (string.IsNullOrWhiteSpace(fileName))
                        continue;

                    if (!prefabLocationsByFileName.TryGetValue(fileName, out List<IResourceLocation> locations))
                    {
                        locations = new List<IResourceLocation>();
                        prefabLocationsByFileName[fileName] = locations;
                    }

                    locations.Add(location);
                }
            }

            for (int i = 0; i < pendingPrefabLocationCacheReady.Count; i++)
            {
                pendingPrefabLocationCacheReady[i]?.Invoke();
            }

            pendingPrefabLocationCacheReady.Clear();
        };
    }

    private IResourceLocation FindBestPrefabLocation(string originalKey, IReadOnlyList<string> candidateKeys)
    {
        string originalFileName = Path.GetFileName(originalKey);
        if (string.IsNullOrWhiteSpace(originalFileName)
            || !prefabLocationsByFileName.TryGetValue(originalFileName, out List<IResourceLocation> locations)
            || locations.Count == 0)
        {
            return null;
        }

        if (locations.Count == 1)
        {
            return locations[0];
        }

        string normalizedOriginalKey = NormalizeKey(originalKey);
        for (int i = 0; i < locations.Count; i++)
        {
            IResourceLocation location = locations[i];
            string primaryKey = NormalizeKey(location.PrimaryKey);
            string internalId = NormalizeKey(location.InternalId);

            if (PathMatches(primaryKey, normalizedOriginalKey, candidateKeys)
                || PathMatches(internalId, normalizedOriginalKey, candidateKeys))
            {
                return location;
            }
        }

        //if (IsValidKey(key) && prefabCache.TryGetValue(key, out var prefab))
        //    return prefab;
        return null;
    }

    private static bool PathMatches(string source, string originalKey, IReadOnlyList<string> candidateKeys)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        if (source.EndsWith(originalKey, StringComparison.OrdinalIgnoreCase))
            return true;

        for (int i = 0; i < candidateKeys.Count; i++)
        {
            string candidateKey = NormalizeKey(candidateKeys[i]);
            if (!string.IsNullOrWhiteSpace(candidateKey)
                && source.EndsWith(candidateKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private void LoadResolvedLocation(
        string requestKey,
        IReadOnlyList<string> candidateKeys,
        IResourceLocation location,
        Action onCompleted,
        Action onFailed)
    {
        AsyncOperationHandle<GameObject> loadHandle = Addressables.LoadAssetAsync<GameObject>(location);
        loadHandle.Completed += loadOperation =>
        {
            onCompleted?.Invoke();

            if (loadOperation.Status != AsyncOperationStatus.Succeeded || loadOperation.Result == null)
            {
                LogLoadFailure(requestKey, candidateKeys, location, loadOperation);
                DeferredAddressablesRelease.Release(loadOperation);
                onFailed?.Invoke();
                return;
            }

            RegisterResolvedPrefab(requestKey, candidateKeys, location, loadOperation);
            FinishLoad(requestKey, loadOperation.Result);
        };
    }

    private void RegisterResolvedPrefab(
        string requestKey,
        IReadOnlyList<string> candidateKeys,
        IResourceLocation location,
        AsyncOperationHandle<GameObject> loadHandle)
    {
        string canonicalKey = NormalizeKey(string.IsNullOrWhiteSpace(location.PrimaryKey)
            ? requestKey
            : location.PrimaryKey);

        if (prefabHandleCache.TryGetValue(canonicalKey, out AsyncOperationHandle<GameObject> cachedHandle)
            && cachedHandle.IsValid())
        {
            DeferredAddressablesRelease.Release(loadHandle);
            return;
        }

        prefabCache[canonicalKey] = loadHandle.Result;
        prefabHandleCache[canonicalKey] = loadHandle;
        aliasToCanonicalKey[canonicalKey] = canonicalKey;
        aliasToCanonicalKey[requestKey] = canonicalKey;

        for (int i = 0; i < candidateKeys.Count; i++)
        {
            aliasToCanonicalKey[NormalizeKey(candidateKeys[i])] = canonicalKey;
        }

        string fileName = Path.GetFileName(canonicalKey);
        if (!string.IsNullOrWhiteSpace(fileName))
        {
            aliasToCanonicalKey[fileName] = canonicalKey;
        }
    }

    private static void LogLoadFailure(
        string requestKey,
        IReadOnlyList<string> candidateKeys,
        IResourceLocation location,
        AsyncOperationHandle<GameObject> loadOperation)
    {
        string primaryKey = location != null ? NormalizeKey(location.PrimaryKey) : string.Empty;
        string internalId = location != null ? NormalizeKey(location.InternalId) : string.Empty;
        string candidates = candidateKeys == null ? string.Empty : string.Join(", ", candidateKeys);
        string exceptionMessage = loadOperation.OperationException != null
            ? loadOperation.OperationException.ToString()
            : "No operation exception provided.";

        Debug.LogError(
            $"[PoolAddressableManager] Addressable prefab load exception.\n" +
            $"RequestKey: {requestKey}\n" +
            $"PrimaryKey: {primaryKey}\n" +
            $"InternalId: {internalId}\n" +
            $"Candidates: {candidates}\n" +
            $"{exceptionMessage}");
    }

    private void FinishLoad(string requestKey, GameObject prefab)
    {
        pendingLoads.Remove(requestKey);

        if (pendingCallbacks.TryGetValue(requestKey, out List<Action<GameObject>> callbacks))
        {
            pendingCallbacks.Remove(requestKey);

            for (int i = 0; i < callbacks.Count; i++)
            {
                callbacks[i]?.Invoke(prefab);
            }
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[PoolAddressableManager] Addressable prefab load failed: {requestKey}");
        }
    }

    private bool TryGetCachedObject(string key, out GameObject prefab)
    {
        return TryGetCachedObject(BuildCandidateKeys(key), out prefab);
    }

    private bool TryGetCachedObject(IReadOnlyList<string> candidateKeys, out GameObject prefab)
    {
        for (int i = 0; i < candidateKeys.Count; i++)
        {
            string candidateKey = NormalizeKey(candidateKeys[i]);

            if (aliasToCanonicalKey.TryGetValue(candidateKey, out string canonicalKey)
                && prefabCache.TryGetValue(canonicalKey, out prefab)
                && prefab != null)
            {
                return true;
            }

            if (prefabCache.TryGetValue(candidateKey, out prefab) && prefab != null)
            {
                return true;
            }
        }

        prefab = null;
        return false;
    }

    private static List<string> BuildCandidateKeys(string key)
    {
        List<string> candidates = new List<string>();
        string normalizedKey = NormalizeKey(key);

        AddCandidate(candidates, normalizedKey);
        AddCandidate(candidates, normalizedKey.Replace("/Skill/", "/SKill/", StringComparison.OrdinalIgnoreCase));

        if (normalizedKey.StartsWith(PrefabRootPath, StringComparison.OrdinalIgnoreCase))
        {
            string relativePath = normalizedKey.Substring(PrefabRootPath.Length);
            AddCandidate(candidates, relativePath);
            AddCandidate(candidates, relativePath.Replace("/Skill/", "/SKill/", StringComparison.OrdinalIgnoreCase));
        }

        AddCandidate(candidates, Path.GetFileName(normalizedKey));

        return candidates;
    }

    private static void AddCandidate(List<string> candidates, string candidate)
    {
        string normalizedCandidate = NormalizeKey(candidate);
        if (string.IsNullOrWhiteSpace(normalizedCandidate) || normalizedCandidate == "0")
            return;

        if (!candidates.Contains(normalizedCandidate))
        {
            candidates.Add(normalizedCandidate);
        }
    }

    private static string GetPrimaryAlias(string key)
    {
        string normalizedKey = NormalizeKey(key);
        string fileName = Path.GetFileName(normalizedKey);
        return string.IsNullOrWhiteSpace(fileName) ? normalizedKey : fileName;
    }

    private static string NormalizeKey(string key)
    {
        return string.IsNullOrWhiteSpace(key)
            ? string.Empty
            : key.Replace('\\', '/').Trim();
    }

    private bool IsValidKey(string key)
    {
        return !string.IsNullOrEmpty(key) && key != "0";
    }

    protected override void OnDestroy()
    {
        foreach (AsyncOperationHandle<GameObject> handle in prefabHandleCache.Values)
        {
            DeferredAddressablesRelease.Release(handle);
        }

        if (prefabLocationsHandle.IsValid())
        {
            DeferredAddressablesRelease.Release(prefabLocationsHandle);
        }

        prefabHandleCache.Clear();
        prefabCache.Clear();
        aliasToCanonicalKey.Clear();
        pendingCallbacks.Clear();
        pendingLoads.Clear();
        prefabLocationsByFileName.Clear();
        pendingPrefabLocationCacheReady.Clear();
        prefabLocationCacheLoaded = false;
        prefabLocationCacheLoading = false;

        base.OnDestroy();
    }
}
