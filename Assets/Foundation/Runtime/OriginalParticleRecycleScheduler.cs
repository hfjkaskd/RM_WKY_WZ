using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // Typed replacement for the recovered Invoke("RecycleSelf", delay).
    // The level owns this queue so disabling a pooled object does not silently
    // cancel its pending call. Manager.Clear explicitly cancels its entries.
    public sealed class OriginalParticleRecycleScheduler
    {
        private struct Entry { public OriginalItemParticles Target; public float Due; }
        private readonly List<Entry> entries = new List<Entry>();
        public void Schedule(OriginalItemParticles target, float now, float delay)
        {
            entries.Add(new Entry { Target = target, Due = now + delay });
        }
        public void Cancel(OriginalItemParticles target)
        {
            for (int i = entries.Count - 1; i >= 0; i--)
                if (entries[i].Target == target) entries.RemoveAt(i);
        }
        public void Tick(float now)
        {
            for (int i = 0; i < entries.Count;)
            {
                var entry = entries[i];
                if (entry.Target == null) { entries.RemoveAt(i); continue; }
                if (now < entry.Due) { i++; continue; }
                entries.RemoveAt(i);
                entry.Target.RecycleSelf();
            }
        }
        public void Clear() { entries.Clear(); }
    }
}
