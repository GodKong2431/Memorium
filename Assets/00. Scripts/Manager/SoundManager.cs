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
    [Range(0f, 1f)] [SerializeField] private float defaultMasterVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultBgmVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultCombatSfxVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float defaultUiSfxVolume = 1f;
    #endregion

    #region Runtime Fields
    private readonly Dictionary<int, SoundEntry> soundById = new Dictionary<int, SoundEntry>();
    private readonly List<AudioSource> uiSources = new List<AudioSource>();
    private readonly List<AudioSource> combatSources = new List<AudioSource>();

    private AudioSource bgmSource;
    private DataManager subscribedDataManager;
    private int nextUiSourceIndex;
    private int nextCombatSourceIndex;
    private float currentBgmClipVolume = 1f;
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
    #endregion

    #region Public API
    public void RebuildLibrary()
    {
        soundById.Clear();

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
                clipVolume = Mathf.Clamp01(table.soundVolume)
            };
        }
    }

    public bool PlayBgm(int soundId)
    {
        return TryResolve(soundId, out SoundEntry entry) && PlayBgm(entry);
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmClipVolume = 1f;
    }

    public bool PlayUiSfx(int soundId)
    {
        return TryResolve(soundId, out SoundEntry entry) && PlayUi(entry);
    }

    public bool PlayCombatSfx(int soundId)
    {
        return TryResolve(soundId, out SoundEntry entry) && PlayCombat(entry);
    }

    public bool PlayCombatSfxAt(int soundId, Vector3 worldPos)
    {
        return TryResolve(soundId, out SoundEntry entry) && PlayCombatAt(entry, worldPos);
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        SaveVolumeSettings();
        ApplyBgmVolume();
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

        if (bgmSource == null)
        {
            bgmSource = CreateChildSource("BGM_Source");
            bgmSource.loop = true;
        }

        CleanupSources(uiSources);
        CleanupSources(combatSources);

        EnsureSourceCount(uiSources, uiSourceCount, "UI_SFX_Source_");
        EnsureSourceCount(combatSources, combatSourceCount, "Combat_SFX_Source_");
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

    private bool PlayUi(SoundEntry entry)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        AudioSource source = GetNextSource(uiSources, ref nextUiSourceIndex);
        if (source == null)
            return false;

        source.PlayOneShot(clip, GetEffectiveVolume(entry.clipVolume, uiSfxVolume));
        return true;
    }

    private bool PlayCombat(SoundEntry entry)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        AudioSource source = GetNextSource(combatSources, ref nextCombatSourceIndex);
        if (source == null)
            return false;

        source.PlayOneShot(clip, GetEffectiveVolume(entry.clipVolume, combatSfxVolume));
        return true;
    }

    private bool PlayCombatAt(SoundEntry entry, Vector3 worldPos)
    {
        if (!TryLoadClip(entry, out AudioClip clip))
            return false;

        AudioSource.PlayClipAtPoint(clip, worldPos, GetEffectiveVolume(entry.clipVolume, combatSfxVolume));
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

    private float GetEffectiveVolume(float clipVolume, float categoryVolume)
    {
        return Mathf.Clamp01(clipVolume) * masterVolume * categoryVolume;
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

    private void RaiseVolumeChanged()
    {
        OnVolumeChanged?.Invoke();
    }
    #endregion
}
