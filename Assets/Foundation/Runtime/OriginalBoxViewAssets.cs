using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // Original BoxItemAssets.sprites order. Large sprites stay path-loaded.
    public sealed class OriginalBoxViewAssets : ScriptableObject
    {
        public string[] spritePaths;
        public string[] audioClipPaths;
        public float tapSoundDelay, tapPhaseDuration, tapMinScale;
        public float[] tapRotationDegrees;
        [Serializable] public sealed class AlternativeClip { public int Type; public string Path; }
        public AlternativeClip[] alternativeClips;
        [NonSerialized] private Sprite[] sprites;
        [NonSerialized] private AudioClip[] audioClips;
        [NonSerialized] private Dictionary<int, AudioClip> alternatives;
        public void InitializeAudio()
        {
            if (audioClips == null)
            {
                audioClips = new AudioClip[audioClipPaths.Length];
                for (int i = 0; i < audioClips.Length; i++) audioClips[i] = Resources.Load<AudioClip>(audioClipPaths[i]);
            }
            if (alternatives == null) alternatives = new Dictionary<int, AudioClip>();
            // ItemAssets.LoadAlternativeAudioClips 0x2b445c0. Converted resource
            // paths replace only the recovered bundle transport. Missing
            // downloaded variants retain the native original-clip fallback.
            if (alternativeClips == null) return;
            for (int i = 0; i < alternativeClips.Length; i++)
            {
                var entry = alternativeClips[i];
                if (alternatives.ContainsKey(entry.Type)) continue;
                var clip = Resources.Load<AudioClip>(entry.Path);
                if (clip != null) alternatives.Add(entry.Type, clip);
            }
        }
        public AudioClip GetAlternativeAudioClip(int type, int originalIndex) // 0x2b44b44.
        {
            if (alternatives == null) throw new NullReferenceException();
            if (alternatives.TryGetValue(type, out var clip)) return clip;
            return originalIndex < 0 ? null : audioClips[originalIndex];
        }
        public AudioClip GetSfxClip(int type) // GameSfxAssets.GetClip box cases 0x1ee9054.
        {
            switch (type)
            {
                case 33: return audioClips[0];
                case 34: case 35: return GetAlternativeAudioClip(type, 0);
                case 36: return audioClips[1];
                case 37: case 38: return GetAlternativeAudioClip(type, 1);
                case 39: return GetAlternativeAudioClip(type, -1);
                default: throw new NotSupportedException("Audio item catalog is not restored for type: " + type);
            }
        }
        public Sprite GetSpriteForLayer(int layer) // 0x2bc5fa8.
        {
            if (sprites == null) sprites = new Sprite[spritePaths.Length];
            int index = unchecked(layer - 1);
            if (sprites[index] == null)
            {
                sprites[index] = Resources.Load<Sprite>(spritePaths[index]);
                if (sprites[index] == null) throw new InvalidOperationException("Missing box sprite: " + spritePaths[index]);
            }
            return sprites[index];
        }
    }
}
