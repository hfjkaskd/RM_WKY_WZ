using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalTntTntComboFuseParticles : OriginalItemParticles, IOriginalTntFuseParticles
    {
        public ParticleSystem fuseParticles;
        public Renderer sorting0, sorting1, sorting2, sorting3, sorting4;
        public Transform Transform => transform;
        public override int GetPoolId() => 18; // 0x27b9bc0.
        public void Play() { if (!fuseParticles.isPlaying) fuseParticles.Play(); }
        public void Stop() { fuseParticles.Stop(); }
        public void Recycle() { RecycleSelf(); }
        public void UpdateSorting(OriginalSwapSorting sorting) // 0x27b9c14.
        {
            OriginalItemSortingRules.Apply(sorting0, sorting);
            OriginalItemSortingRules.Apply(sorting1, OriginalItemSortingRules.WithOffset(sorting, 1));
            OriginalItemSortingRules.Apply(sorting2, OriginalItemSortingRules.WithOffset(sorting, 2));
            OriginalItemSortingRules.Apply(sorting3, OriginalItemSortingRules.WithOffset(sorting, 3));
            OriginalItemSortingRules.Apply(sorting4, OriginalItemSortingRules.WithOffset(sorting, 4));
        }
    }
}
