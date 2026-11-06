using UnityEngine;

namespace Whitebox.Foundation
{
    // Cell SwapParticles 0x3448374/0x3448418. Playback is the inherited
    // masterless optimized emission used by CellModel.SwapStarted 0x319db80.
    public sealed class OriginalSwapParticles : OriginalOptimizedItemParticles
    {
        public ParticleSystem circleParticle;
        [SerializeField] private int itemSortingLayer;
        protected override void Awake()
        {
            base.Awake();
            var renderer = circleParticle.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerID = itemSortingLayer;
            renderer.sortingOrder = -1;
        }
        public override int GetPoolId() => 1;
    }
}
