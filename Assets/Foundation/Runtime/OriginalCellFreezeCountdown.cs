using System;

namespace Whitebox.Foundation
{
    // CellView freeze state +0x38 / +0x48, StartCountdownForFreeze 0x344737c.
    // The owner invokes Update with the shared level delta, as CellView does.
    public sealed class OriginalCellFreezeCountdown
    {
        private readonly Action unfreeze;
        private float remaining, extension;
        public float Remaining => remaining;
        public bool IsCounting { get; private set; }

        public OriginalCellFreezeCountdown(Action unfreeze) { this.unfreeze = unfreeze; }
        public void StartCountdown(float duration) { remaining = duration; IsCounting = true; }
        internal void ResetOnRecycle() { remaining = 0f; IsCounting = false; }
        internal bool ExtendActiveCountdown(float duration)
        {
            // ARM64 b.LS is an ordered <= check; NaN takes the active branch.
            if (remaining <= 0f) return false;
            extension = duration - remaining;
            if (!(extension <= 0f)) remaining += extension;
            return true;
        }
        public void Update(float levelDeltaTime) // 0x3446be4.
        {
            if (!IsCounting) return;
            remaining -= levelDeltaTime;
            // b.GT skips completion only for an ordered positive remainder.
            if (remaining > 0f) return;
            remaining = 0f;
            if (unfreeze == null) throw new NullReferenceException();
            unfreeze();
            IsCounting = false;
        }
    }

    public partial class OriginalGridCell
    {
        public OriginalCellFreezeCountdown FreezeCountdown { get; private set; }
        public void BindFreezeCountdown(OriginalCellFreezeCountdown countdown) { FreezeCountdown = countdown; }
        public void FreezeFallForDuration(float duration) // 0x3190664.
        {
            var countdown = FreezeCountdown;
            if (countdown == null) throw new NullReferenceException();
            if (countdown.ExtendActiveCountdown(duration)) return;
            FreezeFall();
            if (FreezeCountdown == null) throw new NullReferenceException();
            FreezeCountdown.StartCountdown(duration);
        }
    }
}
