using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable] public sealed class OriginalBallBallViewSettings
    {
        public string fastState;
        public float animatorSpeed,fastAnimatorSpeed,initialMaskScale,startMaskScale,endMaskScale,maskDuration;
        public float blackAlpha,fadeDuration,soundDelay;
        public OriginalTntShakeConfig shake;
    }
    // LightballItemAssets.GetSpriteFromMatchType 0x1f3fe28, original slots
    // [1,2,4,6,5,3]. Paths keep the atlas data out of startup strong references.
    public sealed class OriginalLightballViewAssets : ScriptableObject
    {
        public string[] backgroundSpritePaths;
        public float colorBackgroundScale, whiteBackgroundScale, purpleWhiteBackgroundScale;
        public bool speedIncrease;
        [Min(0f)] public float chargeFadeSeconds;
        public float rayDelay, fastRayDelay, rayInitialWidthScale, rayReachDistance;
        public int rayReachFrames, rayFinishFrames, rayFadeStartFrames, rayFadeEndFrames;
        public int fastRayFinishFrames, fastRayFadeStartFrames, fastRayFadeEndFrames;
        public float rayWhiteWidth15, rayColorWidth15, rayWhiteWidth24, rayColorWidth24, rayWhiteWidth3, rayColorWidth3;
        public float positionOffset, positionOffsetDuration, minShakeScale, maxShakeScale, scaleSpeed, shakeTolerance;
        public string useState;
        public float rocketPositionOffset, rocketMaxShakeScale, rocketConversionStartTime, rocketExplodeDelay;
        public float propellerExplodeDelay;
        public OriginalBallBallViewSettings ballBall;
        [NonSerialized] private Sprite[] backgrounds;
        public Sprite GetBackgroundSprite(int matchType)
        {
            if (matchType < 1 || matchType > 6) return null;
            if (backgrounds == null) backgrounds = new Sprite[backgroundSpritePaths.Length];
            int index = matchType - 1;
            if (backgrounds[index] == null)
            {
                backgrounds[index] = Resources.Load<Sprite>(backgroundSpritePaths[index]);
                if (backgrounds[index] == null) throw new InvalidOperationException("Missing lightball background sprite: " + backgroundSpritePaths[index]);
            }
            return backgrounds[index];
        }
        // The source backgrounds/effected-particles use the same ARM64 jump
        // table, with an index0 fallback outside MatchType1..6.
        public static int GetColorIndex(int matchType)
        {
            switch (matchType)
            {
                case 2: return 1;
                case 3: return 3;
                case 4: return 5;
                case 5: return 4;
                case 6: return 2;
                default: return 0;
            }
        }
    }
}
