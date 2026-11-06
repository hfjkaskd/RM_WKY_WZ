using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalVaseVisualAssets : ScriptableObject
    {
        public Sprite[] layerSprites;
        public float[] tapAngles, shakeAngles, shakeDurations;
        public float tapDuration, tapMinScale, tapMinShadowAlpha;
        public float shakeAngleBase, shakeAngleStep, shakeTimeBase, shakeTimeStep;
        public float tntAngleMultiplier, tntTimeMultiplier;
    }
}
