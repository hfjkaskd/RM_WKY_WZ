using UnityEngine;

namespace Whitebox.Foundation
{
    public interface IOriginalTntFuseParticles
    {
        Transform Transform { get; }
        void Play();
        void Stop();
        void Recycle();
        void UpdateSorting(OriginalSwapSorting sorting);
    }
    // TntItemFuseParticles 0x27b7c04..0x27b7d48.
    public sealed class OriginalTntFuseParticles : OriginalItemParticles, IOriginalTntFuseParticles
    {
        public ParticleSystem fuseParticles;
        public Renderer sorting0, sorting1, sorting2;
        public Transform Transform => transform;
        public override int GetPoolId() => 16;
        public void Play() { if (!fuseParticles.isPlaying) fuseParticles.Play(); }
        public void Stop() { fuseParticles.Stop(); }
        public void Recycle() { RecycleSelf(); }
        public void UpdateSorting(OriginalSwapSorting value)
        {
            OriginalItemSortingRules.Apply(sorting0, value);
            OriginalItemSortingRules.Apply(sorting1, OriginalItemSortingRules.WithOffset(value, 1));
            OriginalItemSortingRules.Apply(sorting2, OriginalItemSortingRules.WithOffset(value, 2));
        }
    }
}
