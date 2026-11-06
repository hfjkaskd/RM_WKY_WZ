using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Recovered GameSfxAssets fallHits/matchExplodes order and swipe. Paths replace bundle
    // transport; only requested clips are loaded and retained by this owner.
    public sealed class OriginalGameSfxAssets : ScriptableObject, IOriginalMatchSfxAssets
    {
        [Serializable] public sealed class AdditionalClip { public int Type; public string ResourcePath; }
        public AdditionalClip[] additionalClips;
        [NonSerialized] private readonly Dictionary<int, AudioClip> additionalCache = new Dictionary<int, AudioClip>();
        public string[] fallHitPaths;
        public string[] matchExplodePaths;
        public string matchSwipePath;
        public OriginalBoxViewAssets boxAssets;
        [NonSerialized] private AudioClip[] fallHits;
        [NonSerialized] private int loadedFallHits;
        [NonSerialized] private AudioClip[] matchExplodes;
        [NonSerialized] private int loadedMatchExplodes;
        [NonSerialized] private AudioClip matchSwipe;
        [NonSerialized] private bool loadedMatchSwipe;
        public int MatchExplodeCount => matchExplodePaths.Length;
        public int GetRandomSfxTypeForMatchExplode(OriginalApplicationRandom random) // 0x1ef2438.
        {
            if (random == null) throw new NullReferenceException();
            int index = random.Next(0, 6);
            return unchecked((uint)index) <= 5 ? index + 247 : 0;
        }
        public AudioClip GetClip(int type) // GameSfxAssets.GetClip 0x1ee9054.
        {
            if (additionalClips != null)
                for (int i = 0; i < additionalClips.Length; i++)
                {
                    if (additionalClips[i].Type != type) continue;
                    if (!additionalCache.TryGetValue(type, out var clip))
                    {
                        clip = Resources.Load<AudioClip>(additionalClips[i].ResourcePath);
                        additionalCache.Add(type, clip);
                    }
                    return clip;
                }
            if (type == 254)
            {
                if (!loadedMatchSwipe) { matchSwipe = Resources.Load<AudioClip>(matchSwipePath); loadedMatchSwipe = true; }
                return matchSwipe;
            }
            if (type >= 247 && type <= 252)
            {
                if (matchExplodes == null) matchExplodes = new AudioClip[matchExplodePaths.Length];
                int index = type - 247;
                if ((loadedMatchExplodes & (1 << index)) == 0)
                { matchExplodes[index] = Resources.Load<AudioClip>(matchExplodePaths[index]); loadedMatchExplodes |= 1 << index; }
                return matchExplodes[index];
            }
            if(type==146 || type==147)
            {
                if(fallHits==null)fallHits=new AudioClip[fallHitPaths.Length];
                int index=type-146;
                if((loadedFallHits&(1<<index))==0)
                { fallHits[index]=Resources.Load<AudioClip>(fallHitPaths[index]);loadedFallHits|=1<<index; }
                return fallHits[index];
            }
            if(boxAssets==null)throw new NullReferenceException();
            return boxAssets.GetSfxClip(type);
        }
    }
}
