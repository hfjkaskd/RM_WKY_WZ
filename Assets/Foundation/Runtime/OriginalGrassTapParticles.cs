using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalGrassTapParticles : OriginalItemParticles
    {
        public ParticleSystem[] parts;
        public override int GetPoolId() => 42;

        protected override void Awake()
        {
            base.Awake();
            int sortingLayer = SortingLayer.NameToID("Particles");
            foreach (var renderer in GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                renderer.sortingLayerID = sortingLayer;
                renderer.sortingOrder = 100;
            }
        }

        public void Play(int layer)
        {
            parts[layer].Play();
            OriginalParticleContext.RecycleScheduler.Schedule(this, Time.time, 3f);
        }
    }
}
