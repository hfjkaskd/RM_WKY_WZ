using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCupboardDoorParticles : OriginalItemParticles
    {
        [SerializeField] private ParticleSystem doorParticles;
        [SerializeField] private OriginalItemParticles3d rightDoorParticles3d, leftDoorParticles3d;
        public override int GetPoolId() => 48;

        public void Play(OriginalSortedParticleDepth depth)
        {
            doorParticles.Play();
            leftDoorParticles3d.Play(depth);
            rightDoorParticles3d.Play(depth);
            OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 3f);
        }
    }
}
