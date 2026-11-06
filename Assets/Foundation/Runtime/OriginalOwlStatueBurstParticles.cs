using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalOwlStatueBurstParticles : OriginalItemParticles
    {
        [SerializeField] private OriginalParticleInfo[] layer0Particles, layer1Particles, layer2Particles;

        protected override void Awake()
        {
            base.Awake();
            InitShadows(layer0Particles);
            InitShadows(layer1Particles);
            InitShadows(layer2Particles);
        }

        public override int GetPoolId() => 54;

        public void Play(int layer, Vector3 position) // 0x2473a28.
        {
            switch (layer)
            {
                case 0: PlayParticles(layer0Particles, position); break;
                case 1: PlayParticles(layer1Particles, position); break;
                case 2: PlayParticles(layer2Particles, position); break;
            }
        }
    }
}
