using System;
using UnityEngine;

namespace Bizza.FlyMoney
{
    [Serializable]
    public struct FlyMoneySettings
    {
        public const int MaxParticlesPerLayer = 64;
        public const float MaxImageSize = 500f;
        [Range(0, MaxParticlesPerLayer)] public int rainCount;
        [Range(0, MaxParticlesPerLayer)] public int flyCount;
        [Range(0.5f, 5f)] public float duration;
        [Range(10f, MaxImageSize)] public float rainSize;
        [Range(10f, MaxImageSize)] public float flySize;
        [Range(20f, 220f)] public float scatterRadius;
        [Range(0f, 120f)] public float fallSpeed;
        public bool trails;
        [Range(2, 8)] public int trailSegments;
        [Range(0.02f, 0.2f)] public float trailTime;
        [Range(2f, 30f)] public float trailWidth;
        public Color trailColor;

        public static FlyMoneySettings Standard => new FlyMoneySettings
        {
            rainCount = 32, flyCount = 28, duration = 1.7f,
            rainSize = 50f, flySize = 34f, scatterRadius = 108f, fallSpeed = 48f,
            trails = true, trailSegments = 6, trailTime = 0.13f,
            trailWidth = 16f, trailColor = new Color(0.94f, 0.22f, 0.69f, 0.42f)
        };

        public static FlyMoneySettings Low => new FlyMoneySettings
        {
            rainCount = 20, flyCount = 20, duration = 1.7f,
            rainSize = 50f, flySize = 34f, scatterRadius = 108f, fallSpeed = 48f,
            trails = true, trailSegments = 4, trailTime = 0.12f,
            trailWidth = 13f, trailColor = new Color(0.94f, 0.22f, 0.69f, 0.36f)
        };

        public FlyMoneySettings Sanitized()
        {
            FlyMoneySettings value = this;
            value.rainCount = Mathf.Clamp(rainCount, 0, MaxParticlesPerLayer);
            value.flyCount = Mathf.Clamp(flyCount, 0, MaxParticlesPerLayer);
            value.duration = ClampFinite(duration, 0.5f, 5f, 1.7f);
            value.rainSize = ClampFinite(rainSize, 10f, MaxImageSize, 50f);
            value.flySize = ClampFinite(flySize, 10f, MaxImageSize, 34f);
            value.scatterRadius = ClampFinite(scatterRadius, 20f, 220f, 108f);
            value.fallSpeed = ClampFinite(fallSpeed, 0f, 120f, 48f);
            value.trailSegments = Mathf.Clamp(trailSegments, 2, 8);
            value.trailTime = ClampFinite(trailTime, 0.02f, 0.2f, 0.13f);
            value.trailWidth = ClampFinite(trailWidth, 2f, 30f, 16f);
            value.trailColor = new Color(
                ClampFinite(trailColor.r, 0f, 1f, 1f),
                ClampFinite(trailColor.g, 0f, 1f, 0.3f),
                ClampFinite(trailColor.b, 0f, 1f, 0.7f),
                ClampFinite(trailColor.a, 0f, 0.65f, 0.4f));
            return value;
        }

        internal static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        internal static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        private static float ClampFinite(float value, float min, float max, float fallback)
            => Finite(value) ? Mathf.Clamp(value, min, max) : fallback;
    }

    public enum FlyMoneyEndReason { Completed, Stopped, TargetLost, Disabled }

    public struct FlyMoneyBurstFrame
    {
        public float radius;
        public float rayAlpha;
        public float sparkleAlpha;
        public float cloudSize;
        public float cloudSpread;
        public float cloudAlpha;
        public float coreAlpha;
        public float cloudSquash;
        public float scale;
        public bool Visible => cloudAlpha + coreAlpha + rayAlpha + sparkleAlpha > 0.001f;
    }

    public struct FlyMoneyFrame
    {
        public Vector2 position;
        public float size;
        public float angle;
        public float alpha;
        public bool flying;
        public bool Visible => alpha > 0.001f && size > 0.001f;
    }
}
