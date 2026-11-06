using UnityEngine;

namespace Whitebox.Foundation
{
    // The creation-particle ownership portion of SpecialItemView, separated
    // from its item-specific animation/renderer implementations. Calls remain
    // typed and use the concrete restored front/back particle components.
    public sealed class OriginalSpecialCreationParticleTransfer
    {
        private readonly Transform owner;
        private readonly OriginalGameObjectPool pool;
        public OriginalSpecialItemCreationParticles CreationParticles { get; private set; }
        public OriginalSpecialCreationParticleTransfer(Transform owner, OriginalGameObjectPool pool)
        {
            this.owner = owner; this.pool = pool;
        }
        public void SetCreationParticles(OriginalSpecialItemCreationParticles particles, bool isAboveStaticSpreading,
            bool isUnderNonFallBlockingAboveStaticItem) // 0x27b4f1c.
        {
            CreationParticles = particles;
            if (!isAboveStaticSpreading && !isUnderNonFallBlockingAboveStaticItem) return;
            DecreaseCreationParticleSorting();
        }
        public void DecreaseCreationParticleSorting() // 0x27b4dcc.
        {
            if (CreationParticles == null) return;
            var snapshot = CreationParticles.GetParticles(owner);
            var back = pool.Spawn<OriginalSpecialItemCreationParticlesBack>(6, true);
            back.Init(owner);
            back.PlayParticles(snapshot);
            CreationParticles.StopParticles(owner, 0, true);
            CreationParticles = null;
        }
    }
}
