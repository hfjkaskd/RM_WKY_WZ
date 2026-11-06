using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // AudioManager ordinary one-shot path. Music, looping/stoppable sources,
    // preferences and context lifecycle are separate native paths.
    public sealed partial class OriginalSfxPlayback
    {
        private readonly Dictionary<int, float> startTimes = new Dictionary<int, float>(10);
        private readonly Func<float> time;
        private readonly Func<int, AudioClip> resolveClip;
        private readonly AudioSource source;
        private readonly Action<int> missingClip;
        public bool SfxMute, OverrideSfxMuteForNightmare;
        public float GetClipLastPlayedTime(int type) // AudioManager 0x3e18f78.
        { return startTimes.TryGetValue(type,out var value)?value:0f; }
        public OriginalSfxPlayback(Func<float> time, Func<int, AudioClip> resolveClip,
            AudioSource source, Action<int> missingClip)
        {
            this.time = time ?? throw new ArgumentNullException(nameof(time));
            this.resolveClip = resolveClip ?? throw new ArgumentNullException(nameof(resolveClip));
            this.source = source;
            this.missingClip = missingClip ?? throw new ArgumentNullException(nameof(missingClip));
        }
        public bool CanPlayAudio(int type, float offset) // 0x3e17770.
        {
            if (SfxMute && !OverrideSfxMuteForNightmare) return false;
            if (startTimes.ContainsKey(type) && time() < startTimes[type] + offset) return false;
            return true;
        }
        public void PlaySound(int type, float offset) // 0x3e17838.
        {
            if (!CanPlayAudio(type, offset)) return;
            startTimes[type] = time();
            PlayResolved(type, resolveClip(type));
        }
        public void PlaySound(int type, AudioClip clip, float offset) // 0x3e17a04.
        {
            if (!CanPlayAudio(type, offset)) return;
            startTimes[type] = time();
            PlayResolved(type, clip);
        }
        private void PlayResolved(int type, AudioClip clip)
        {
            // Unity destroyed-object equality, as in the native object check.
            if (clip == null) { missingClip(type); return; }
            if (ReferenceEquals(source, null)) throw new NullReferenceException();
            source.PlayOneShot(clip);
        }
    }
    public interface IOriginalMatchSfxAssets
    {
        int MatchExplodeCount { get; }
        int GetRandomSfxTypeForMatchExplode(OriginalApplicationRandom random);
    }
    public sealed partial class OriginalGameSfx
    {
        private readonly OriginalSfxPlayback audio;
        private readonly OriginalApplicationRandom random;
        private readonly Func<float> time;
        private bool noGameSound;
        private IOriginalMatchSfxAssets matchAssets;
        internal int matchExplodeCounter;
        public int MatchExplodeCounter => matchExplodeCounter;
        public OriginalGameSfx(OriginalSfxPlayback audio) { this.audio = audio; }
        public OriginalGameSfx(OriginalSfxPlayback audio, OriginalApplicationRandom random, Func<float> time)
        { this.audio=audio;this.random=random;this.time=time; }
        public void SetNoGameSound(bool value) { noGameSound = value; } // 0x2180830.
        public void BindMatchAssets(IOriginalMatchSfxAssets value) { matchAssets = value; }
        public void PlayMatchExplode(bool isSwap) // 0x2180304.
        {
            if (noGameSound) return;
            if (isSwap) matchExplodeCounter = 0;
            else
            {
                int next = unchecked(matchExplodeCounter + 1);
                matchExplodeCounter = next;
                var assets = matchAssets ?? throw new NullReferenceException();
                int maximum = unchecked(assets.MatchExplodeCount - 1);
                matchExplodeCounter = next < 0 ? 0 : Math.Min(next, maximum);
            }
            var current = matchAssets ?? throw new NullReferenceException();
            int type = current.GetRandomSfxTypeForMatchExplode(random);
            if (audio == null) throw new NullReferenceException();
            audio.PlaySound(type, .04f);
        }
        public void PlayFallHit() // 0x218022c.
        {
            if(noGameSound)return;
            if(audio==null)throw new NullReferenceException();
            float first=audio.GetClipLastPlayedTime(146);
            float second=audio.GetClipLastPlayedTime(147);
            // Native b.ls suppresses <= .1, while unordered (NaN) continues.
            if(time()-first<=.1f)return;
            if(time()-second<=.1f)return;
            if(random==null)throw new NullReferenceException();
            int index=random.Next(0,2);
            audio.PlaySound(index==0?146:147,.04f);
        }
        public void PlaySfx(int type, float offset) // 0x21805c0.
        {
            if (noGameSound) return;
            if (audio == null) throw new NullReferenceException();
            audio.PlaySound(type, offset);
        }
        public void PlaySfx(int type, AudioClip clip, float offset) // 0x21805ec.
        {
            if (noGameSound) return;
            if (audio == null) throw new NullReferenceException();
            audio.PlaySound(type, clip, offset);
        }
    }
}
