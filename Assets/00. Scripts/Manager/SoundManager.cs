
using System;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : Singleton<SoundManager>
{
    [Serializable]
    public class SoundEntry
    {
        public string key;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    private AudioSource bgmSource;
    private List<AudioSource> sfxSources = new List<AudioSource>();
    
    [SerializeField] private List<SoundEntry> library = new List<SoundEntry>();
    [SerializeField] private int sfxSourceCount = 4;

    [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1f;

    private readonly Dictionary<string, SoundEntry> soundMap = new Dictionary<string, SoundEntry>();
    private int nextSfxIndex;
    private float currentBgmClipVolume = 1f;

    #region Manage
    protected override void Awake()
    {
        base.Awake();
        ClampVolumes();
        EnsureSources();
        BuildMap();
        ApplySfxVolume();
    }

    // 런타임 중에 라이브러리 바뀔 수 있으서 냅둠
    public void RebuildLibrary()
    {
        BuildMap();
    }

    // 사운드 소스 개수만큼 만들어 놓음
    private void EnsureSources()
    {
        if (sfxSourceCount < 1)
            sfxSourceCount = 1;

        if (bgmSource == null)
            bgmSource = CreateChildSource("BGM_Source");

        for (int i = sfxSources.Count - 1; i >= 0; i--)
        {
            if (sfxSources[i] == null)
                sfxSources.RemoveAt(i);
        }

        while (sfxSources.Count < sfxSourceCount)
        {
            int index = sfxSources.Count;
            sfxSources.Add(CreateChildSource("SFX_Source_" + index));
        }

        ApplySfxVolume();
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
        return source;
    }

    private void BuildMap()
    {
        soundMap.Clear();

        for (int i = 0; i < library.Count; i++)
        {
            SoundEntry entry = library[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.clip == null)
                continue;

            soundMap[entry.key] = entry;
        }
    }

    private bool TryGet(string key, out SoundEntry entry)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            entry = null;
            return false;
        }

        return soundMap.TryGetValue(key, out entry);
    }

    private void ClampVolumes()
    {
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
    }
    #endregion

    #region BGM
    public bool PlayBgm(string key)
    {
        if (!TryGet(key, out SoundEntry entry))
            return false;

        currentBgmClipVolume = entry.volume;
        bgmSource.clip = entry.clip;
        bgmSource.loop = true;
        bgmSource.volume = currentBgmClipVolume * bgmVolume;
        bgmSource.Play();
        return true;
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
        bgmSource.clip = null;
        currentBgmClipVolume = 1f;
    }

    public void SetBgmVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);

        if (bgmSource != null && bgmSource.isPlaying)
            bgmSource.volume = currentBgmClipVolume * bgmVolume;
    }
    #endregion

    #region SFX
    // 2D 기반 SFX 재생(UI, 시스템 효과음 등)
    public bool PlaySfx(string key)
    {
        if (!TryGet(key, out SoundEntry entry))
            return false;

        AudioSource source = GetNextSfxSource();
        if (source == null)
            return false;

        source.PlayOneShot(entry.clip, entry.volume);
        return true;
    }

    // 3D기반 SFX 재생(몬스터 효과음, 스킬 등)
    public bool PlaySfxAt(string key, Vector3 worldPos)
    {
        if (!TryGet(key, out SoundEntry entry))
            return false;

        AudioSource.PlayClipAtPoint(entry.clip, worldPos, entry.volume * sfxVolume);
        return true;
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        ApplySfxVolume();
    }

    // 하나의 SFXSource를 사용하는게 아니라 하나 재생후 다음 Source로 인덱스 변경해서 사용
    private AudioSource GetNextSfxSource()
    {
        if (sfxSources == null || sfxSources.Count == 0)
            return null;

        if (nextSfxIndex >= sfxSources.Count)
            nextSfxIndex = 0;
        AudioSource source = sfxSources[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Count;
        return source;
    }

    private void ApplySfxVolume()
    {
        if (sfxSources == null)
            return;

        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];
            if (source == null)
                continue;

            source.volume = sfxVolume;
        }
    }
    #endregion
}
