using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHoneyTapParticles : OriginalItemParticles
    {
        [SerializeField] private ParticleSystem particles;
        public override int GetPoolId() => 45;

        public void Play() // 0x349bb94.
        {
            particles.Play();
            OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 3f);
        }
    }
}
