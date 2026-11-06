using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalPropellerTargetExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem system;
        public override int GetPoolId() => 12; // 0x27c1128.
        public void Play() // 0x27b24ac.
        { system.Play(); OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 1f); }
        public override void CancelScheduledActions()
        { OriginalParticleContext.RecycleScheduler.Cancel(this); base.CancelScheduledActions(); }
    }
}
