using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable]
    public sealed class OriginalRocketViewPose
    {
        public Vector3 BodyOffset, ShadowOffset, ShadowEuler, FirstPartOffset, SecondPartOffset;
        public int BodySprite, FirstPartSprite, SecondPartSprite;
    }

    // Lightweight replacement for RocketItemAssets' recovered ordered sprite
    // references. Large image data is resolved by path when the strategy asks.
    public sealed class OriginalRocketViewAssets : ScriptableObject
    {
        public string[] spritePaths;
        public string horizontalCreationState;
        public string verticalCreationState;
        public int shadowSprite;
        public int creationSound;
        public float creationSoundOffset;
        public int useSound;
        public float useSoundOffset, speed, accelerationPerFrame, hitDistance;
        public float propellerComboTargetDelay,propellerComboShadowAlpha;
        public float horizontalScreenMargin, verticalScreenMargin, wideDeviceUnfreezeX, wideDeviceAspect, recycleTail;
        public OriginalRocketViewPose horizontalPose, verticalPose;
        [NonSerialized] private Sprite[] sprites;
        [NonSerialized] private int horizontalHash, verticalHash;
        [NonSerialized] private bool hashesInitialized;
        public OriginalRocketViewPose HorizontalPose => horizontalPose;
        public OriginalRocketViewPose VerticalPose => verticalPose;
        public int ShadowSprite => shadowSprite;
        public int CreationSound => creationSound;
        public float CreationSoundOffset => creationSoundOffset;
        public Sprite GetSprite(int index)
        {
            if (sprites == null) sprites = new Sprite[spritePaths.Length];
            if (unchecked((uint)index) >= (uint)spritePaths.Length || sprites.Length != spritePaths.Length)
                throw new InvalidOperationException("Invalid rocket sprite configuration: index=" + index
                    + ", paths=" + spritePaths.Length + ", cache=" + sprites.Length);
            if (sprites[index] == null)
            {
                sprites[index] = Resources.Load<Sprite>(spritePaths[index]);
                if (sprites[index] == null) throw new InvalidOperationException("Missing rocket sprite: " + spritePaths[index]);
            }
            return sprites[index];
        }
        public int GetCreationState(bool vertical)
        {
            if (!hashesInitialized)
            {
                horizontalHash = Animator.StringToHash(horizontalCreationState); // 0x27bd120.
                verticalHash = Animator.StringToHash(verticalCreationState); // 0x27be3e4.
                hashesInitialized = true;
            }
            return vertical ? verticalHash : horizontalHash;
        }
    }
}
