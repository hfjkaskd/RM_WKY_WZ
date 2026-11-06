using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable] public struct OriginalTntShakeConfig
    {
        public float Delay, Duration, MinVibrate, MidVibrate, MaxVibrate;
        public bool ShouldScale, ShouldVisitCenter;
        public int Direction;
    }
    [Serializable] public struct OriginalTntExplosionConfig
    {
        public int Radius;
        public float RingInterval, ExplodeDelay;
        public OriginalTntShakeConfig Shake;
    }
    public sealed class OriginalTntViewAssets : ScriptableObject
    {
        public string spritePath, boosterState, lightballCreationState;
        public Vector3 shadowOffset, recycledMaskOffset, recycledMaskScale;
        public Color shadowColor;
        public float propellerComboShadowAlpha;
        public int creationSound;
        public float creationSoundOffset;
        public OriginalTntExplosionConfig explosion;
        public OriginalTntRocketAnimationSettings rocketCombo;
        [NonSerialized] private Sprite sprite;
        [NonSerialized] private int boosterHash, lightballHash;
        [NonSerialized] private bool hashesInitialized;
        public Sprite GetSprite()
        {
            if (sprite == null)
            {
                sprite = Resources.Load<Sprite>(spritePath);
                if (sprite == null) throw new InvalidOperationException("Missing TNT sprite: " + spritePath);
            }
            return sprite;
        }
        public int GetState(bool lightball)
        {
            if (!hashesInitialized)
            {
                boosterHash = Animator.StringToHash(boosterState);
                lightballHash = Animator.StringToHash(lightballCreationState);
                hashesInitialized = true;
            }
            return lightball ? lightballHash : boosterHash;
        }
    }
}
