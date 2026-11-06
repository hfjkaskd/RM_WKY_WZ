using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalEggBurstParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] particles;
        protected override void Awake()
        {
            base.Awake();
            InitShadows(particles);
        }
        public override int GetPoolId() => 52;
        public void Play(Vector3 position) => PlayParticles(particles, position);
    }
}
