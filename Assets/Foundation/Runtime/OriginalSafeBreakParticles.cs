using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalSafeBreakParticles : OriginalItemParticles
    {
        [SerializeField] private ParticleSystem baseParticles;
        public override int GetPoolId() => 67;

        public void Play() // 0x2f120a4.
        {
            baseParticles.Play();
            OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 3f);
        }
    }
}
