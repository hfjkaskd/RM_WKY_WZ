using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPropellerItemExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle;
        public override int GetPoolId() => 11; // 0x27c07e8.
        public void Play() // 0x27b1fac. Typed scheduler replaces Invoke(string).
        { circleParticle.Play(); OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 1f); }
        public override void CancelScheduledActions()
        { OriginalParticleContext.RecycleScheduler.Cancel(this); base.CancelScheduledActions(); }
    }
}
