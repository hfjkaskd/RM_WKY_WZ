using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalHoneyBurstParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] particleInfos;
        public override int GetPoolId() => 44;

        protected override void Awake()
        {
            base.Awake();
            InitShadows(particleInfos);
        }

        public void Play(Vector3 position) => PlayParticles(particleInfos, position);
    }
}
