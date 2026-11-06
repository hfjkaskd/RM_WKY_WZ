using System;

namespace Whitebox.Foundation
{
    // DeltaTimeManager 0x318d1c4 / 0x318d334. Unity delta is already scaled.
    public sealed class OriginalLevelClock
    {
        private float lastDelta;
        public int FrameCount { get; private set; }
        public float Time { get; private set; }
        public static float QuantizeDelta(float delta)
        {
            float scaled = delta * 10000f;
            // ARM64 fcvtms rounds down and saturates; positive infinity has
            // an explicit native fallback to int.MinValue / 10000.
            int value;
            if (float.IsNaN(scaled)) value = 0;
            else if (float.IsPositiveInfinity(scaled) || scaled <= int.MinValue) value = int.MinValue;
            else if (scaled >= 2147483648f) value = int.MaxValue;
            else value = (int)Math.Floor(scaled);
            return value / 10000f;
        }
        public void ManualLateUpdate(float unityDeltaTime)
        {
            lastDelta = QuantizeDelta(unityDeltaTime);
            Time += lastDelta;
            FrameCount = unchecked(FrameCount + 1);
        }
    }
}
