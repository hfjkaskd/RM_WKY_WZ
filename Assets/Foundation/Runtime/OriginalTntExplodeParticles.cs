using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntExplodeParticles : OriginalItemParticles
    {
        public OriginalParticleInfo[] particleInfos;
        public ParticleSystem blast, blast2, smoke, glare, ring;
        public override int GetPoolId() => 19;
        public void Play(Vector3 position, bool reduced) // 0x27b7a48.
        {
            SetCount(blast, reduced ? (short)5 : (short)25);
            SetCount(blast2, reduced ? (short)5 : (short)25);
            SetCount(smoke, reduced ? (short)3 : (short)15);
            SetCount(glare, reduced ? (short)1 : (short)2);
            SetCount(ring, reduced ? (short)1 : (short)2);
            PlayParticles(particleInfos, position);
        }
        private static void SetCount(ParticleSystem system, short count) // 0x27b7b24.
        {
            var emission = system.emission; var burst = emission.GetBurst(0);
            burst.minCount = count; burst.maxCount = count; emission.SetBurst(0, burst);
        }
    }
}
