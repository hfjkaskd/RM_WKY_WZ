using System;

namespace Whitebox.Foundation
{
    public interface IOriginalUnityTimeSource
    {
        float DeltaTime { get; }
        float TimeScale { get; set; }
    }
    // DeltaTimeManager 0x318d1b4..0x318d36c. ReplayDeltaTimeManager is a
    // separate implementation; the ordinary clock never caches frame delta.
    public sealed class OriginalDeltaTimeManager : IOriginalLevelContextClock
    {
        private readonly IOriginalUnityTimeSource unityTime;
        private readonly OriginalLevelClock clock = new OriginalLevelClock();
        private float timeScale;
        public int FrameCount => clock.FrameCount;
        public float DeltaTime => OriginalLevelClock.QuantizeDelta(unityTime.DeltaTime);
        public float Time => clock.Time;
        public float TimeScale => timeScale;
        public OriginalDeltaTimeManager(IOriginalGameLateUpdateRegistration updates, IOriginalUnityTimeSource unityTime)
        {
            this.unityTime = unityTime ?? throw new ArgumentNullException(nameof(unityTime));
            // Native constructor registers first, then reads Unity timeScale.
            updates.RegisterToLateUpdate(ManualLateUpdate);
            timeScale = unityTime.TimeScale;
        }
        public void SetTimeScale(float value)
        {
            timeScale = value; // Retained even when Unity rejects the write.
            unityTime.TimeScale = value;
        }
        internal void ManualLateUpdate() { clock.ManualLateUpdate(unityTime.DeltaTime); }
    }
}
