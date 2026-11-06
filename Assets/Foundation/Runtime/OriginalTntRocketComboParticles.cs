using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntRocketComboParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle;
        public override int GetPoolId() => 20;
        public void Play() // 0x27ba28c, strongly typed replacement for Invoke("RecycleSelf", 1).
        { circleParticle.Play(); OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 1f); }
        public override void CancelScheduledActions()
        { OriginalParticleContext.RecycleScheduler.Cancel(this); base.CancelScheduledActions(); }
    }
}
