using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntPropellerComboFuseParticles : OriginalItemParticles, IOriginalTntFuseParticles
    {
        public ParticleSystem fuseParticles;
        public Renderer sorting0, sorting1, sorting2, sorting3;
        public Transform Transform => transform;
        public override int GetPoolId() => 17; // 0x27ba378.
        public void Play() { if (!fuseParticles.isPlaying) fuseParticles.Play(); }
        public void Stop() { fuseParticles.Stop(); }
        public void Recycle() { RecycleSelf(); }
        public void UpdateSorting(OriginalSwapSorting value) // 0x27ba3cc.
        {
            OriginalItemSortingRules.Apply(sorting0,value);
            OriginalItemSortingRules.Apply(sorting1,OriginalItemSortingRules.WithOffset(value,1));
            OriginalItemSortingRules.Apply(sorting2,OriginalItemSortingRules.WithOffset(value,2));
            OriginalItemSortingRules.Apply(sorting3,OriginalItemSortingRules.WithOffset(value,3));
        }
    }
}
