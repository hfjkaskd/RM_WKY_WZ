using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalSafeDamageParticles : OriginalItemParticles
    {
        [SerializeField] private ParticleSystem handleParticles, doorParticles1, doorParticles2, doorParticles3;
        [SerializeField] private OriginalItemParticles3d handle3dParticles, door3dParticles3;
        public override int GetPoolId() => 66;

        public void Play(int layer, OriginalSortedParticleDepth depth) // 0x2f12118.
        {
            switch (layer)
            {
                case 1:
                    doorParticles3.Play();
                    door3dParticles3.Play(depth);
                    break;
                case 2: doorParticles2.Play(); break;
                case 3: doorParticles1.Play(); break;
                case 4:
                    handleParticles.Play();
                    handle3dParticles.Play(depth);
                    break;
            }
            OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 2f);
        }
    }
}
