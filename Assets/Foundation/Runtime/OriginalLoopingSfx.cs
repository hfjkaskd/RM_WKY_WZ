using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed partial class OriginalSfxPlayback
    {
        private OriginalGameObjectPool loopPool;
        private readonly Dictionary<int, int> loopCounts = new Dictionary<int, int>();
        private readonly Dictionary<int, Queue<OriginalPoolableAudioSource>> loopSources = new Dictionary<int, Queue<OriginalPoolableAudioSource>>();
        private readonly Dictionary<long, OriginalPoolableAudioSource> stoppableSources = new Dictionary<long, OriginalPoolableAudioSource>();
        private readonly List<FadingSound> fadingSounds = new List<FadingSound>();
        private long nextStoppableKey;

        private struct FadingSound
        {
            public OriginalPoolableAudioSource Source;
            public float StartVolume, Duration, Elapsed;
        }

        internal long PlayStoppableSound(int type, MonoBehaviour coroutineHost, bool loop = false) // AudioManager 0x3e17fe8/0x3e18030.
        {
            if (SfxMute && !OverrideSfxMuteForNightmare) return 0;
            var clip = resolveClip(type);
            if (clip == null) { missingClip(type); return 0; }
            var pooled = loopPool.Spawn<OriginalPoolableAudioSource>(0, true);
            long key = ++nextStoppableKey;
            stoppableSources.Add(key, pooled);
            var audioSource = pooled.audioSource;
            audioSource.loop = loop;
            audioSource.volume = 1f;
            audioSource.clip = clip;
            audioSource.mute = SfxMute;
            audioSource.Play();
            if (!loop) coroutineHost.StartCoroutine(ReleaseWhenFinished(key));
            return key;
        }

        private IEnumerator ReleaseWhenFinished(long key) // 0x3e19c40.
        {
            while (stoppableSources.TryGetValue(key, out var pooled) && pooled.audioSource.isPlaying)
                yield return null;
            StopSound(key);
        }

        internal void StopSound(long key) // 0x3e18268.
        {
            if (!stoppableSources.TryGetValue(key, out var pooled)) return;
            stoppableSources.Remove(key);
            loopPool.Recycle(pooled);
        }

        internal void FadeOutSound(long key, float duration) // AudioManager 0x3e186fc.
        {
            if (!stoppableSources.TryGetValue(key, out var pooled)) return;
            stoppableSources.Remove(key);
            fadingSounds.Add(new FadingSound { Source = pooled, StartVolume = pooled.audioSource.volume, Duration = duration });
        }
        internal void TickFadingSounds(float deltaTime)
        {
            for (int i = fadingSounds.Count - 1; i >= 0; i--)
            {
                var fade = fadingSounds[i];
                fade.Elapsed += deltaTime;
                if (fade.Elapsed < fade.Duration)
                {
                    fade.Source.audioSource.volume = Mathf.Lerp(fade.StartVolume, 0f, fade.Elapsed / fade.Duration);
                    fadingSounds[i] = fade;
                    continue;
                }
                loopPool.Recycle(fade.Source);
                fadingSounds.RemoveAt(i);
            }
        }

        public void BindLoopPool(OriginalGameObjectPool value) { loopPool = value ?? throw new ArgumentNullException(nameof(value)); }
        public int GetLoopCount(int type) => loopCounts.TryGetValue(type, out var count) ? count : 0;
        public void PlaySoundInLoop(int type) => TryPlaySoundInLoop(type);
        internal bool TryPlaySoundInLoop(int type) // AudioManager 0x3e17c50.
        {
            if (SfxMute && !OverrideSfxMuteForNightmare) return false;
            int count = unchecked(GetLoopCount(type) + 1);
            if (count <= 5)
            {
                var clip = resolveClip(type);
                if (clip == null) { missingClip(type); return false; }
                var pooled = loopPool.Spawn<OriginalPoolableAudioSource>(0, true);
                if (!loopSources.TryGetValue(type, out var queue))
                { queue = new Queue<OriginalPoolableAudioSource>(); loopSources.Add(type, queue); }
                queue.Enqueue(pooled);
                pooled.audioSource.loop = true;
                pooled.audioSource.clip = clip;
                pooled.audioSource.mute = SfxMute;
                pooled.audioSource.Play();
                startTimes[type] = time();
            }
            loopCounts[type] = count;
            return true;
        }
        public void StopSoundInLoop(int type) // AudioManager 0x3e18dc4.
        {
            if (!loopCounts.TryGetValue(type, out int count)) return;
            int remaining = unchecked(count - 1);
            if (remaining < 0) return;
            if (count <= 5)
            {
                var pooled = loopSources[type].Dequeue();
                if (pooled != null) loopPool.Recycle(pooled);
            }
            loopCounts[type] = remaining;
            if (remaining == 0) loopCounts.Remove(type);
        }
        internal void StopAllSounds()
        {
            source.Stop();
            loopPool.RecycleAllSpawned();
            loopCounts.Clear();
            loopSources.Clear();
            stoppableSources.Clear();
            fadingSounds.Clear();
        }
        internal void SetSfxMuted(bool muted)
        {
            SfxMute = muted;
            source.mute = muted;
            foreach (var queue in loopSources.Values)
                foreach (var pooled in queue)
                    pooled.audioSource.mute = muted;
            foreach (var pooled in stoppableSources.Values) pooled.audioSource.mute = muted;
            for (int i = 0; i < fadingSounds.Count; i++) fadingSounds[i].Source.audioSource.mute = muted;
        }
    }
    public sealed partial class OriginalGameSfx
    {
        public void PlaySfxInLoop(int type) => TryPlaySfxInLoop(type);
        internal bool TryPlaySfxInLoop(int type) => !noGameSound && audio.TryPlaySoundInLoop(type); // 0x2180618.
        internal long PlayStoppableSfx(int type, MonoBehaviour coroutineHost, bool loop) =>
            noGameSound ? 0 : audio.PlayStoppableSound(type, coroutineHost, loop); // 0x2180678.
        public void StopSoundInLoop(int type) => audio.StopSoundInLoop(type); // 0x21806e8.
    }
}
