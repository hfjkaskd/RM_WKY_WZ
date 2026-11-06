using System.Collections.Generic;
using UnityEngine;


public class SoundManager : BaseGameModule<SoundManager>
{
    [Header("Audio Sources")]
    public AudioSource bgmSource;
    public AudioSource sfxSourcePrefab;

    private readonly Dictionary<string, AudioClip> bgmDict = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, AudioClip> sfxDict = new Dictionary<string, AudioClip>();
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, float> sfxSourceVolumeScales = new Dictionary<AudioSource, float>();

    [Header("Settings")]
    [Range(0, 1)] public float bgmVolume = 1f;
    [Range(0, 1)] public float sfxVolume = 1f; 
    public bool isMuted = false;
    public bool bgmMuted = false;
    public bool sfxMuted = false;

    private static readonly Dictionary<string, float> LastPlayTime = new Dictionary<string, float>();

    public void PlayBGM(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (bgmSource.clip != null && bgmSource.clip.name == name)
        {
            return;
        }

        if (!bgmDict.ContainsKey(name))
        {
            bgmDict.Add(name, Resources.Load<AudioClip>("Audios/" + name));
        }

        if (bgmDict.TryGetValue(name, out var clip) && clip != null)
        {
            if (bgmSource.clip != clip)
            {
                bgmSource.clip = clip;
                bgmSource.loop = true;
                bgmSource.volume = (isMuted || bgmMuted) ? 0f : bgmVolume;
                bgmSource.Play();
            }

            return;
        }

        Debug.LogWarning($"BGM clip '{name}' not found!");
    }

    public void StopBGM()
    {
        bgmSource.Stop();
    }

    public void PlaySFX(E_SoundType soundType, float minInterval = 0.12f)
    {
        PlaySFX(soundType.ToString(), minInterval);
    }

    public void PlaySFX(string name, float minInterval = 0.12f)
    {
        PlaySFX(name, minInterval, 1f);
    }

    public void PlaySFX(string name, float minInterval, float volumeScale)
    {
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        if (!CanPlaySFX(name, minInterval))
        {
            return;
        }

        var clip = GetOrLoadSFXClip(name);
        if (clip != null)
        {
            PlaySFXClipInternal(clip, volumeScale);
            return;
        }

        Debug.LogWarning($"SFX clip '{name}' not found!");
    }

    public void PlaySFX(AudioClip clip, float minInterval = 0.12f, float volumeScale = 1f)
    {
        if (clip == null)
        {
            return;
        }

        if (!CanPlaySFX(clip.name, minInterval))
        {
            return;
        }

        PlaySFXClipInternal(clip, volumeScale);
    }

    public AudioSource PlayLoopSFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
        {
            return null;
        }

        var source = GetAvailableSFXSource();
        source.clip = clip;
        source.loop = true;
        ApplySFXSourceVolume(source, volumeScale);
        source.Play();
        return source;
    }

    public AudioSource PlayLoopSFX(string name, float volumeScale = 1f)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        var clip = GetOrLoadSFXClip(name);
        if (clip == null)
        {
            Debug.LogWarning($"Loop SFX clip '{name}' not found!");
            return null;
        }

        return PlayLoopSFX(clip, volumeScale);
    }

    public void StopSFX(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.loop = false;
        source.clip = null;
        sfxSourceVolumeScales[source] = 1f;
    }

    public void SetBGMVolume(float volume)
    {
        bgmVolume = volume;
        bgmSource.volume = (isMuted || bgmMuted) ? 0f : bgmVolume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        for (var i = 0; i < sfxSources.Count; i++)
        {
            var source = sfxSources[i];
            ApplySFXSourceVolume(source, GetSFXSourceVolumeScale(source));
        }
    }

    public void MuteAll(bool mute)
    {
        isMuted = mute;
        UpdateAllVolumes();
    }

    public void MuteBGM(bool mute)
    {
        bgmMuted = mute;
        bgmSource.volume = (isMuted || bgmMuted) ? 0f : bgmVolume;
    }

    public void MuteSFX(bool mute)
    {
        sfxMuted = mute;
        for (var i = 0; i < sfxSources.Count; i++)
        {
            var source = sfxSources[i];
            ApplySFXSourceVolume(source, GetSFXSourceVolumeScale(source));
        }
    }

    public bool IsBGMPlaying() => bgmSource.isPlaying;
    public bool IsBGMMuted() => isMuted || bgmMuted;
    public bool IsSFXMuted() => isMuted || sfxMuted;

    private void UpdateAllVolumes()
    {
        bgmSource.volume = (isMuted || bgmMuted) ? 0f : bgmVolume;
        for (var i = 0; i < sfxSources.Count; i++)
        {
            var source = sfxSources[i];
            ApplySFXSourceVolume(source, GetSFXSourceVolumeScale(source));
        }
    }

    private AudioSource GetAvailableSFXSource()
    {
        for (var i = 0; i < sfxSources.Count; i++)
        {
            var source = sfxSources[i];
            if (!source.isPlaying)
            {
                return source;
            }
        }

        var newSource = Instantiate(sfxSourcePrefab, transform);
        sfxSources.Add(newSource);
        sfxSourceVolumeScales[newSource] = 1f;
        return newSource;
    }

    private static bool CanPlaySFX(string key, float minInterval)
    {
        if (!LastPlayTime.ContainsKey(key))
        {
            LastPlayTime.Add(key, 0f);
        }

        if (Time.realtimeSinceStartup - LastPlayTime[key] < minInterval)
        {
            return false;
        }

        LastPlayTime[key] = Time.realtimeSinceStartup;
        return true;
    }

    private AudioClip GetOrLoadSFXClip(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (!sfxDict.ContainsKey(name))
        {
            sfxDict.Add(name, Resources.Load<AudioClip>("Audios/" + name));
        }

        return sfxDict.TryGetValue(name, out var clip) ? clip : null;
    }

    private void PlaySFXClipInternal(AudioClip clip, float volumeScale)
    {
        var source = GetAvailableSFXSource();
        source.loop = false;
        source.clip = clip;
        ApplySFXSourceVolume(source, volumeScale);
        source.Play();
    }

    private void ApplySFXSourceVolume(AudioSource source, float volumeScale)
    {
        if (source == null)
        {
            return;
        }

        sfxSourceVolumeScales[source] = volumeScale;
        source.volume = (isMuted || sfxMuted) ? 0f : sfxVolume * volumeScale;
    }

    private float GetSFXSourceVolumeScale(AudioSource source)
    {
        if (source == null)
        {
            return 1f;
        }

        return sfxSourceVolumeScales.TryGetValue(source, out var volumeScale) ? volumeScale : 1f;
    }
}
 [Obfuz.ObfuzIgnore]
public enum E_SoundType
{
    BtnClick,
    Win,
    Lose,
    ClickElement,
    SynthesisFull,
    Synthesising,
}
