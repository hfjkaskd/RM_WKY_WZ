using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle;
        public override int GetPoolId() => 28;
        public void Play(float now)
        {
            circleParticle.Play();
            OriginalParticleContext.RecycleScheduler.Schedule(this, now, 1f);
        }
    }
}
