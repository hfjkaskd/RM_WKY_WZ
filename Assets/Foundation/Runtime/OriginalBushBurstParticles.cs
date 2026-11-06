using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalBushBurstParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] layer1Particles, layer2Particles, layer3Particles, layer4Particles, layer5Particles;

        protected override void Awake()
        {
            base.Awake();
            InitShadows(layer1Particles);
            InitShadows(layer2Particles);
            InitShadows(layer3Particles);
            InitShadows(layer4Particles);
            InitShadows(layer5Particles);
        }

        public override int GetPoolId() => 63;

        public void Play(int layer, Vector3 position) // 0x2bc4674.
        {
            switch (layer)
            {
                case 0: PlayParticles(layer1Particles, position); break;
                case 1: PlayParticles(layer2Particles, position); break;
                case 2: PlayParticles(layer3Particles, position); break;
                case 3: PlayParticles(layer4Particles, position); break;
                case 4: PlayParticles(layer5Particles, position); break;
            }
        }
    }
}
