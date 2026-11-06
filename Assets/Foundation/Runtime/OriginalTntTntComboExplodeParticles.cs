using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntTntComboExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle;
        public override int GetPoolId() => 22; // 0x27ba27c.
        public void Play() // 0x27ba1f8. Typed call replaces Invoke("RecycleSelf", 1).
        { circleParticle.Play(); OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 1f); }
        public override void CancelScheduledActions()
        { OriginalParticleContext.RecycleScheduler.Cancel(this); base.CancelScheduledActions(); }
    }
}
