using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    #region Types
    public enum SoundCategory
    {
        Unknown = 0,
        Bgm = 1,
        Ui = 2,
        Combat = 3
    }

    private sealed class SoundEntry
    {
        public SoundCategory category;
        public string resourcePath;
        public float clipVolume;
        public AudioClip clip;
    }
    #endregion

    #region Constants
    private const string MasterVolumeKey = "sound.master";
    private const string BgmVolumeKey = "sound.bgm";
    private const string CombatSfxVolumeKey = "sound.combat";
    private const string UiSfxVolumeKey = "sound.ui";
    #endregion

    #region Inspector
    [SerializeField] private int uiSourceCount = 4;
    [SerializeField] private int combatSourceCount = 8;
    [SerializeField] private int combatWorldSourceCount = 6;
    [SerializeField] private int combatWorldSourceMaxCount = 20;
    [SerializeField] private float combatWorldMinDistance = 5f;
    [SerializeField] private float combatWorldMaxDistance = 30f;
    [Header("Rapid Repeat Guard")]
    [Min(0f)] [SerializeField] private float defaultUiSfxDuplicateBlockSeconds = 0f;
    [Min(0f)] [SerializeField] private float defaultCombatSfxDuplicateBlockSeconds = 0f;
    [SerializeField] private SoundVolDb volDb;
    [Range(0f, 1f)] [SerializeField] private float defaultMasterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultBgmVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultCombatSfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultUiSfxVolume = 1f;
    #endregion

    #region Runtime Fields
    private readonly Dictionary<int, SoundEntry> soundById = new Dictionary<int, SoundEntry>();
    private readonly Dictionary<int, float> sfxBlockedUntilById = new Dictionary<int, float>();
    private readonly List<AudioSource> uiSources = new List<AudioSource>();
    private readonly List<AudioSource> combatSources = new List<AudioSource>();
    private readonly List<AudioSource> activeCombatWorldSources = new List<AudioSource>();

    private AudioSource bgmSource;
    private AudioSource combatLoopSource;
    private GameObject combatWorldSourceTemplate;
    private DataManager subscribedDataManager;
    private int nextUiSourceIndex;
    private int nextCombatSourceIndex;
    private int totalCombatWorldSourceCount;
    private int currentBgmSoundId;
    private int currentCombatLoopSoundId;
    private float currentBgmClipVolume = 1f;
    private float currentCombatLoopClipVolume = 1f;
    private float masterVolume;
    private float bgmVolume;
    private float combatSfxVolume;
    private float uiSfxVolume;
    #endregion

    #region Properties & Events
    public float MasterVolume => masterVolume;
    public float BgmVolume => bgmVolume;
    public float CombatSfxVolume => combatSfxVolume;
    public float UiSfxVolume => uiSfxVolume;

    public event Action OnVolumeChanged;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        LoadVolumeSettings();
        LoadVolDb();
        EnsureSources();
        SubscribeDataManager();
        RebuildLibrary();
        ApplyBgmVolume();
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        SubscribeDataManager();
    }

    protected override void OnDestroy()
    {
        UnsubscribeDataManager();
        base.OnDestroy();
    }

    private void Update()
    {
        if (Instance != this)
            return;

        RecycleFinishedCombatWorldSources();
    }
    #endregion

    #region Public API
    public void RebuildLibrary()
    {
        soundById.Clear();
        LoadVolDb();

        var soundDict = DataManager.Instance?.SoundDict;
        if (soundDict == null || soundDict.Count == 0)
            return;

        foreach (var pair in soundDict)
        {
            SoundTable table = pair.Value;
            if (table == null)
                continue;

            string resourcePath = NormalizeToResourcePath(table.soundPath);
            if (string.IsNullOrWhiteSpace(resourcePath))
                continue;

            soundById[pair.Key] = new SoundEntry
            {
                category = ResolveCategory(table.typeId),
                resourcePath = resourcePath,
                clipVolume = ResolveClipVolume(pair.Key, table.soundVolume)
            };
        }

        RefreshLiveClips();
    }

    public bool PlayBgm(int soundId)
    {
        if (!TryResolve(soundId, out SoundEntry entry) || !PlayBgm(entry))
            return false;

        currentBgmSoundId = soundId;
        return true;
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmSoundId = 0;
        currentBgmClipVolume = 1f;
    }

    public bool PlayUiSfx(int soundId, float minIntervalSeconds = -1f)
    {
        float effectiveMinInterval = ResolveDuplicateBlockSeconds(minIntervalSeconds, defaultUiSfxDuplicateBlockSeconds);
        return TryResolve(soundId, out SoundEntry entry) && PlayUi(soundId, entry, effectiveMinInterval);
    }

    public bool PlayCombatSfx(int soundId, float minIntervalSeconds = -1f)
    {
        float effectiveMinInterval = ResolveDuplicateBlockSeconds(minIntervalSeconds, defaultCombatSfxDuplicateBlockSeconds);
        return TryResolve(soundId, out SoundEntry entry) && PlayCombat(soundId, entry, effectiveMinInterval);
    }

    public bool PlayCombatSfxAt(int soundId, Vector3 worldPos, float minIntervalSeconds = -1f)
    {
        float effectiveMinInterval = ResolveDuplicateBlockSeconds(minIntervalSeconds, defaultCombatSfxDuplicateBlockSeconds);
        return TryResolve(soundId, out SoundEntry entry) && PlayCombatAt(soundId, entry, worldPos, effectiveMinInterval);
    }

    public bool PlayCombatLoopSfx(int soundId)
    {
        if (!TryResolve(soundId, out SoundEntry entry) || !PlayCombatLoop(entry))
            return false;

        currentCombatLoopSoundId = soundId;
        return true;
    }

    public void StopCombatLoopSfx()
    {
        if (combatLoopSource == null)
            return;

        combatLoopSource.Stop();
        combatLoopSource.clip = null;
        currentCombatLoopSoundId = 0;
        currentCombatLoopClipVolume = 1f;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
        ApplyBgmVolume();
        ApplyCombatLoopVolume();
        RaiseVolumeChanged();
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
        ApplyBgmVolume();
        RaiseVolumeChanged();
    }

    public void SetCombatSfxVolume(float volume)
    {
        combatSfxVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
        ApplyCombatLoopVolume();
        RaiseVolumeChanged();
    }

    public void SetUiSfxVolume(float volume)
    {
        uiSfxVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
        RaiseVolumeChanged();
    }
    #endregion

    #region Data Binding
    private void SubscribeDataManager()
    {
        DataManager dataManager = DataManager.Instance;
        if (dataManager == null || subscribedDataManager == dataManager)
            return;

        UnsubscribeDataManager();
        subscribedDataManager = dataManager;
        subscribedDataManager.OnComplete += HandleDataLoaded;

        if (subscribedDataManager.DataLoad)
            RebuildLibrary();
    }

    private void UnsubscribeDataManager()
    {
        if (subscribedDataManager == null)
            return;

        subscribedDataManager.OnComplete -= HandleDataLoaded;
        subscribedDataManager = null;
    }

    private void HandleDataLoaded()
    {
        RebuildLibrary();
    }
    #endregion

    #region Source Setup
    private void EnsureSources()
    {
        if (uiSourceCount < 1)
            uiSourceCount = 1;

        if (combatSourceCount < 1)
            combatSourceCount = 1;

        if (combatWorldSourceCount < 1)
            combatWorldSourceCount = 1;

        if (combatWorldSourceMaxCount < combatWorldSourceCount)
            combatWorldSourceMaxCount = combatWorldSourceCount;

        combatWorldMinDistance = Mathf.Max(0.1f, combatWorldMinDistance);
        combatWorldMaxDistance = Mathf.Max(combatWorldMinDistance, combatWorldMaxDistance);

        if (bgmSource == null)
        {
            bgmSource = CreateChildSource("BGM_Source");
            bgmSource.loop = true;
        }

        if (combatLoopSource == null)
        {
            combatLoopSource = CreateChildSource("Combat_Loop_Source");
            combatLoopSource.loop = true;
        }

        EnsureCombatWorldSourceTemplate();
        CleanupSources(uiSources);
        CleanupSources(combatSources);
        CleanupCombatWorldSources();

        EnsureSourceCount(uiSources, uiSourceCount, "UI_SFX_Source_");
        EnsureSourceCount(combatSources, combatSourceCount, "Combat_SFX_Source_");
        PrewarmCombatWorldSources(combatWorldSourceCount);
    }

    private void CleanupSources(List<AudioSource> sources)
    {
        for (int i = sources.Count - 1; i >= 0; i--)
        {
            if (sources[i] == null)
                sources.RemoveAt(i);
        }
    }

    private void EnsureSourceCount(List<AudioSource> sources, int requiredCount, string namePrefix)
    {
        while (sources.Count < requiredCount)
        {
            AudioSource source = CreateChildSource(namePrefix + sources.Count);
            sources.Add(source);
        }
    }

    private void CleanupCombatWorldSources()
    {
        for (int i = activeCombatWorldSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeCombatWorldSources[i];
            if (source == null)
                activeCombatWorldSources.RemoveAt(i);
        }
    }

    private void EnsureCombatWorldSourceTemplate()
    {
        if (combatWorldSourceTemplate != null)
            return;

        combatWorldSourceTemplate = new GameObject("Combat_World_SFX_Source_Template");
        combatWorldSourceTemplate.transform.SetParent(transform, false);
        combatWorldSourceTemplate.hideFlags = HideFlags.HideInHierarchy;

        AudioSource source = combatWorldSourceTemplate.AddComponent<AudioSource>();
        ConfigureCombatWorldSource(source);
    }

    private void PrewarmCombatWorldSources(int requiredCount)
    {
        if (combatWorldSourceTemplate == null || totalCombatWorldSourceCount >= requiredCount)
            return;

        int createCount = requiredCount - totalCombatWorldSourceCount;
        List<GameObject> borrowedObjects = new List<GameObject>(createCount);
        for (int i = 0; i < createCount; i++)
        {
            GameObject pooledObject = ObjectPoolManager.Get(combatWorldSourceTemplate, Vector3.zero, Quaternion.identity);
            if (pooledObject == null)
                break;

            borrowedObjects.Add(pooledObject);
            totalCombatWorldSourceCount++;
        }

        for (int i = 0; i < borrowedObjects.Count; i++)
            ObjectPoolManager.Return(borrowedObjects[i]);
    }

    private AudioSource CreateChildSource(string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform);
        child.transform.localPosition = Vector3.zero;

        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        source.volume = 1f;
        return source;
    }

    private void ConfigureCombatWorldSource(AudioSource source)
    {
        if (source == null)
            return;

        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.minDistance = combatWorldMinDistance;
        source.maxDistance = combatWorldMaxDistance;
        source.dopplerLevel = 0f;
    }
    #endregion

    #region Playback
    private bool PlayBgm(SoundEntry entry)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        bgmSource.clip = clip;
        bgmSource.loop = true;
        currentBgmClipVolume = entry.clipVolume;
        ApplyBgmVolume();
        bgmSource.Play();
        return true;
    }

    private bool PlayUi(int soundId, SoundEntry entry, float minIntervalSeconds)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        AudioSource source = GetNextSource(uiSources, ref nextUiSourceIndex);
        if (source == null)
            return false;

        if (IsSfxDuplicateBlocked(soundId, minIntervalSeconds))
            return false;

        source.PlayOneShot(clip, GetEffectiveVolume(entry.clipVolume, uiSfxVolume));
        RegisterSfxPlayback(soundId, minIntervalSeconds);
        return true;
    }

    private bool PlayCombat(int soundId, SoundEntry entry, float minIntervalSeconds)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        AudioSource source = GetNextSource(combatSources, ref nextCombatSourceIndex);
        if (source == null)
            return false;

        if (IsSfxDuplicateBlocked(soundId, minIntervalSeconds))
            return false;

        source.PlayOneShot(clip, GetEffectiveVolume(entry.clipVolume, combatSfxVolume));
        RegisterSfxPlayback(soundId, minIntervalSeconds);
        return true;
    }

    private bool PlayCombatAt(int soundId, SoundEntry entry, Vector3 worldPos, float minIntervalSeconds)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        AudioSource source = GetCombatWorldSource();
        if (source == null)
            return false;

        if (IsSfxDuplicateBlocked(soundId, minIntervalSeconds))
        {
            ResetCombatWorldSource(source);
            ObjectPoolManager.Return(source.gameObject);
            return false;
        }

        ConfigureCombatWorldSource(source);
        source.transform.position = worldPos;
        source.clip = clip;
        source.volume = GetEffectiveVolume(entry.clipVolume, combatSfxVolume);
        source.Play();
        activeCombatWorldSources.Add(source);
        RegisterSfxPlayback(soundId, minIntervalSeconds);
        return true;
    }

    private bool PlayCombatLoop(SoundEntry entry)
    {
        if (combatLoopSource == null)
            return false;

        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        combatLoopSource.clip = clip;
        combatLoopSource.loop = true;
        currentCombatLoopClipVolume = entry.clipVolume;
        ApplyCombatLoopVolume();

        if (!combatLoopSource.isPlaying)
            combatLoopSource.Play();

        return true;
    }

    #endregion

    #region Lookup
    private AudioSource GetNextSource(List<AudioSource> sources, ref int index)
    {
        if (sources == null || sources.Count == 0)
            return null;

        if (index >= sources.Count)
            index = 0;

        AudioSource source = sources[index];
        index = (index + 1) % sources.Count;
        return source;
    }

    private AudioSource GetCombatWorldSource()
    {
        RecycleFinishedCombatWorldSources();

        if (combatWorldSourceTemplate == null)
            return null;

        if (activeCombatWorldSources.Count < totalCombatWorldSourceCount)
        {
            return SpawnCombatWorldSource();
        }

        if (totalCombatWorldSourceCount < combatWorldSourceMaxCount)
        {
            AudioSource source = SpawnCombatWorldSource();
            if (source != null)
                totalCombatWorldSourceCount++;
            return source;
        }

        if (activeCombatWorldSources.Count > 0)
        {
            AudioSource source = activeCombatWorldSources[0];
            activeCombatWorldSources.RemoveAt(0);

            if (source != null)
                ObjectPoolManager.Return(source.gameObject);

            return SpawnCombatWorldSource();
        }

        return null;
    }

    private AudioSource SpawnCombatWorldSource()
    {
        GameObject pooledObject = ObjectPoolManager.Get(combatWorldSourceTemplate, Vector3.zero, Quaternion.identity);
        if (pooledObject == null)
            return null;

        AudioSource source = pooledObject.GetComponent<AudioSource>();
        if (source == null)
            return null;

        ResetCombatWorldSource(source);
        return source;
    }

    private void RecycleFinishedCombatWorldSources()
    {
        for (int i = activeCombatWorldSources.Count - 1; i >= 0; i--)
        {
            AudioSource source = activeCombatWorldSources[i];
            if (source == null)
            {
                activeCombatWorldSources.RemoveAt(i);
                continue;
            }

            if (source.isPlaying)
                continue;

            activeCombatWorldSources.RemoveAt(i);
            ObjectPoolManager.Return(source.gameObject);
        }
    }

    private void ResetCombatWorldSource(AudioSource source)
    {
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
        source.loop = false;
        source.volume = 1f;
        ConfigureCombatWorldSource(source);
        source.transform.localPosition = Vector3.zero;
    }

    private bool TryResolve(int soundId, out SoundEntry entry)
    {
        entry = null;
        if (soundId <= 0)
            return false;

        if (soundById.Count == 0)
            RebuildLibrary();

        return soundById.TryGetValue(soundId, out entry);
    }

    private bool TryLoadClip(SoundEntry entry, out AudioClip clip)
    {
        clip = null;
        if (entry == null)
            return false;

        if (entry.clip != null)
        {
            clip = entry.clip;
            return true;
        }

        if (string.IsNullOrWhiteSpace(entry.resourcePath))
            return false;

        entry.clip = Resources.Load<AudioClip>(entry.resourcePath);
        clip = entry.clip;
        return clip != null;
    }
    #endregion

    #region Helpers
    private static SoundCategory ResolveCategory(int typeId)
    {
        return typeId switch
        {
            1 => SoundCategory.Bgm,
            2 => SoundCategory.Ui,
            3 => SoundCategory.Combat,
            _ => SoundCategory.Unknown
        };
    }

    private static string NormalizeToResourcePath(string rawPath)
    {
        if (string.IsNullOrWhiteSpace(rawPath))
            return string.Empty;

        string normalized = rawPath.Trim().Replace('\\', '/');
        const string legacyResourcesPrefix = "Assets/00. resources/";

        if (normalized.StartsWith(legacyResourcesPrefix, StringComparison.OrdinalIgnoreCase))
            normalized = "Assets/Resources/" + normalized.Substring(legacyResourcesPrefix.Length);

        if (normalized.StartsWith("Assets/Resources/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("Assets/Resources/".Length);
        else if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
            normalized = normalized.Substring("Resources/".Length);

        string extension = Path.GetExtension(normalized);
        if (!string.IsNullOrWhiteSpace(extension))
            normalized = normalized.Substring(0, normalized.Length - extension.Length);

        return normalized.Trim('/');
    }

    private void LoadVolDb()
    {
        if (volDb != null)
            return;

        volDb = Resources.Load<SoundVolDb>(SoundVolDb.ResPath);
    }

    private float ResolveClipVolume(int soundId, float baseVolume)
    {
        if (volDb != null && volDb.TryGetVol(soundId, out float tunedVolume))
            return tunedVolume;

        return Mathf.Clamp01(baseVolume);
    }

    private void RefreshLiveClips()
    {
        if (currentBgmSoundId > 0 && soundById.TryGetValue(currentBgmSoundId, out SoundEntry bgmEntry))
        {
            currentBgmClipVolume = bgmEntry.clipVolume;
            ApplyBgmVolume();
        }

        if (currentCombatLoopSoundId > 0 && soundById.TryGetValue(currentCombatLoopSoundId, out SoundEntry loopEntry))
        {
            currentCombatLoopClipVolume = loopEntry.clipVolume;
            ApplyCombatLoopVolume();
        }
    }

    private float GetEffectiveVolume(float clipVolume, float categoryVolume)
    {
        return Mathf.Clamp01(clipVolume) * masterVolume * categoryVolume;
    }

    private static float ResolveDuplicateBlockSeconds(float requestedSeconds, float defaultSeconds)
    {
        return requestedSeconds >= 0f ? requestedSeconds : Mathf.Max(0f, defaultSeconds);
    }

    private bool IsSfxDuplicateBlocked(int soundId, float minIntervalSeconds)
    {
        if (soundId <= 0 || minIntervalSeconds <= 0f)
            return false;

        float now = Time.unscaledTime;
        return sfxBlockedUntilById.TryGetValue(soundId, out float blockedUntil) && now < blockedUntil;
    }

    private void RegisterSfxPlayback(int soundId, float minIntervalSeconds)
    {
        if (soundId <= 0 || minIntervalSeconds <= 0f)
            return;

        sfxBlockedUntilById[soundId] = Time.unscaledTime + minIntervalSeconds;
    }
    #endregion

    #region Volume
    private void LoadVolumeSettings()
    {
        masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, defaultMasterVolume));
        bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, defaultBgmVolume));
        combatSfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(CombatSfxVolumeKey, defaultCombatSfxVolume));
        uiSfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(UiSfxVolumeKey, defaultUiSfxVolume));
    }

    private void SaveVolumeSettings()
    {
        PlayerPrefs.SetFloat(MasterVolumeKey, masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, bgmVolume);
        PlayerPrefs.SetFloat(CombatSfxVolumeKey, combatSfxVolume);
        PlayerPrefs.SetFloat(UiSfxVolumeKey, uiSfxVolume);
        PlayerPrefs.Save();
    }

    private void ApplyBgmVolume()
    {
        if (bgmSource == null)
            return;

        bgmSource.volume = GetEffectiveVolume(currentBgmClipVolume, bgmVolume);
    }

    private void ApplyCombatLoopVolume()
    {
        if (combatLoopSource == null)
            return;

        combatLoopSource.volume = GetEffectiveVolume(currentCombatLoopClipVolume, combatSfxVolume);
    }

    private void RaiseVolumeChanged()
    {
        OnVolumeChanged?.Invoke();
    }
    #endregion
}
