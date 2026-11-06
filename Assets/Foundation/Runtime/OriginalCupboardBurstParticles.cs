using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalCupboardBurstParticles : OriginalItemParticles
    {
        [SerializeField] private int poolId;
        [SerializeField] private OriginalParticleInfo[] particles;

        protected override void Awake()
        {
            base.Awake();
            InitShadows(particles);
        }

        public override int GetPoolId() => poolId;
        public void Play(Vector3 position) => PlayParticles(particles, position);
    }
}
