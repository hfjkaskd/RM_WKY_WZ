using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // The authored StartScene sources keep ordinary game music and sound effects separate.
    public sealed class OriginalGameAudioHost : MonoBehaviour
    {
        [SerializeField] private AudioSource soundSource, musicSource;
        [SerializeField] private string firstLevelMusicPath;
        [SerializeField] private string[] additionalMusicPaths;
        private static string previousMusicPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetMusicSelection() => previousMusicPath = null;
        [SerializeField] private OriginalBoxViewAssets boxAssets;
        [SerializeField] private OriginalGameSfxAssets gameAssets;
        [SerializeField] private string loopPoolRootPath, loopSourcePrefabPath;
        [SerializeField] private int loopSourcePrewarmCount;
        private OriginalGameObjectPool loopPool;
        private float soundVolume;
        private const string MusicMutedKey = "CoreMusicMuted";
        private const string SoundMutedKey = "CoreSoundMuted";
        private string currentMusicPath;
        private bool musicRequested;
        public OriginalApplicationRandom Random { get; private set; }
        public OriginalSfxPlayback Playback { get; private set; }
        public OriginalGameSfx GameSfx { get; private set; }
        public AudioSource SoundSource => soundSource;
        public AudioSource MusicSource => musicSource;
        public OriginalBoxViewAssets BoxAssets => boxAssets;
        public OriginalGameSfxAssets GameAssets => gameAssets;
        internal bool MusicMuted => musicSource.mute;
        internal bool SoundMuted => Playback.SfxMute;
        public void Initialize()
        {
            if (GameSfx != null) throw new InvalidOperationException("Game audio already initialized.");
            if (soundSource == null || musicSource == null || boxAssets == null || gameAssets==null) throw new InvalidOperationException("Game audio prefab is not configured.");
            soundVolume = soundSource.volume;
            Random = OriginalApplicationRandom.Shared;
            boxAssets.InitializeAudio();
            Playback = new OriginalSfxPlayback(() => Time.time, gameAssets.GetClip, soundSource,
                type => Debug.LogError("Missing original audio clip type: " + type));
            var rootPrefab = Resources.Load<GameObject>(loopPoolRootPath);
            var sourcePrefab = Resources.Load<GameObject>(loopSourcePrefabPath);
            if (rootPrefab == null || sourcePrefab == null) throw new InvalidOperationException("Loop audio pool prefabs are missing.");
            loopPool = new OriginalGameObjectPool(1, Instantiate(rootPrefab).transform);
            loopPool.CreatePool(sourcePrefab.GetComponent<OriginalPoolableAudioSource>(), loopSourcePrewarmCount); // AudioManager.Bind 0x3e14058: seven sources.
            Playback.BindLoopPool(loopPool);
            musicSource.mute = PlayerPrefs.GetInt(MusicMutedKey, 0) != 0;
            Playback.SetSfxMuted(PlayerPrefs.GetInt(SoundMutedKey, 0) != 0);
            GameSfx = new OriginalGameSfx(Playback,Random,() => Time.time);
            GameSfx.BindMatchAssets(gameAssets);
        }
        // AudioManager.SelectGameMusic 0x3e15aac uses the built-in track for
        // level 1 and draws from the installed tracks plus the built-in one otherwise.
        private string SelectGameMusic(int level)
        {
            if (level == 1 || additionalMusicPaths.Length == 0) return firstLevelMusicPath;
            for (int attempt = 0; attempt < 9; attempt++)
            {
                int index = Random.Next(additionalMusicPaths.Length + 1);
                string path = index < additionalMusicPaths.Length ? additionalMusicPaths[index] : firstLevelMusicPath;
                if (path != previousMusicPath) return path;
            }
            return previousMusicPath;
        }

        internal void PlayGameMusic(int level)
        {
            musicSource.Stop();
            musicSource.clip = null;
            currentMusicPath = previousMusicPath = SelectGameMusic(level);
            musicRequested = true;
            if (!MusicMuted) PlaySelectedMusic();
        }

        private void PlaySelectedMusic()
        {
            var clip = Resources.Load<AudioClip>(currentMusicPath);
            if (clip == null) throw new InvalidOperationException("Missing original game music: " + currentMusicPath);
            musicSource.clip = clip;
            musicSource.Play();
        }

        internal void StopGameMusic(bool pause = false)
        {
            if (musicSource == null) return;
            musicRequested = false;
            if (pause) musicSource.Pause();
            else musicSource.Stop();
        }
        internal void ToggleMusic()
        {
            bool muted = !MusicMuted;
            musicSource.mute = muted;
            PlayerPrefs.SetInt(MusicMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
            if (muted) musicSource.Pause();
            else if (musicRequested)
            {
                if (musicSource.clip == null) PlaySelectedMusic();
                else musicSource.UnPause();
            }
        }
        internal void ToggleSound()
        {
            bool muted = !SoundMuted;
            Playback.SetSfxMuted(muted);
            PlayerPrefs.SetInt(SoundMutedKey, muted ? 1 : 0);
            PlayerPrefs.Save();
        }
        internal void ResetAttemptSounds()
        {
            Playback.StopAllSounds();
            soundSource.volume = soundVolume;
            GameSfx.SetNoGameSound(false);
        }

        private void OnDestroy() { if (loopPool != null && loopPool.IsPoolAlive()) loopPool.Destroy(); }
    }
}
