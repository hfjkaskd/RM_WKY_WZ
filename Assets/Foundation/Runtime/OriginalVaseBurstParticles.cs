using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalVaseBurstParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] layer1Particles, layer2Particles;

        protected override void Awake()
        {
            base.Awake();
            InitShadows(layer1Particles);
            InitShadows(layer2Particles);
        }

        public override int GetPoolId() => 39;
        public void Play(int remainingLayers, Vector3 position) =>
            PlayParticles(remainingLayers == 1 ? layer1Particles : layer2Particles, position);
    }
}
