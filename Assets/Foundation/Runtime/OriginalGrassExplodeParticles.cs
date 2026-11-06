using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGrassExplodeParticles : OriginalItemParticles
    {
        public OriginalParticleInfo[] layer2, layer1;

        protected override void Awake()
        {
            base.Awake();
            int sortingLayer = SortingLayer.NameToID("Particles");
            foreach (var renderer in GetComponentsInChildren<ParticleSystemRenderer>())
            {
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = 100;
            }
            InitShadows(layer2);
            InitShadows(layer1);
        }

        public override int GetPoolId() => 41;
        public void Play(int layer, Vector3 position) => PlayParticles(layer > 0 ? layer2 : layer1, position);
    }
}
