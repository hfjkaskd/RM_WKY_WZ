using UnityEngine;

namespace Whitebox.Foundation
{
    // BoxItemExplodeParticles 0x2bc6164/61a8/61f8. Original layer arrays and
    // shadow components are preserved in the recovered configured prefab.
    public sealed class OriginalBoxExplodeParticles : OriginalItemParticles
    {
        public OriginalParticleInfo[] layer4, layer3, layer2, layer1;
        protected override void Awake()
        {
            base.Awake();
            InitShadows(layer1); InitShadows(layer2); InitShadows(layer3); InitShadows(layer4);
        }
        public override int GetPoolId() => 37;
        public void Play(int particleLayer, Vector3 position)
        {
            switch (particleLayer)
            {
                case 0: PlayParticles(layer1, position); break;
                case 1: PlayParticles(layer2, position); break;
                case 2: PlayParticles(layer3, position); break;
                case 3: PlayParticles(layer4, position); break;
            }
        }
    }
}
