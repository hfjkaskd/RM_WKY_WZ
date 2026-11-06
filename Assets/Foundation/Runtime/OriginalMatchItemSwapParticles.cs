using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // MatchItemSwapParticles 0x21a9338..0x21a9654. The four directional
    // systems and their children/modules are configured in the recovered prefab.
    public sealed class OriginalMatchItemSwapParticles : OriginalItemParticles
    {
        public ParticleSystem topParticles, bottomParticles, leftParticles, rightParticles;
        private Renderer[] renderers;
        private OriginalParticleRecycleScheduler recycleScheduler;
        protected override void Awake()
        {
            base.Awake();
            renderers = GetComponentsInChildren<Renderer>();
            recycleScheduler = OriginalParticleContext.RecycleScheduler;
        }
        public override int GetPoolId() => 3;
        public void Init(Transform parent) // Native factory parameter is unused.
        {
            transform.SetParent(parent);
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        public ParticleSystem GetParticleForDirection(int direction)
        {
            switch (direction)
            {
                case 1: return topParticles;
                case 3: return rightParticles;
                case 5: return bottomParticles;
                case 7: return leftParticles;
                default: return null;
            }
        }
        public void Play(int direction)
        {
            var particle = GetParticleForDirection(direction);
            if (particle == null) throw new NullReferenceException();
            particle.Play(); // Native default includes children.
            recycleScheduler.Schedule(this, Time.time, 1f);
        }
        public void UpdateSorting(OriginalSwapSorting sorting) =>
            OriginalItemSortingRules.UpdateParticleRenderers(renderers, sorting);
        public override void CancelScheduledActions()
        {
            base.CancelScheduledActions();
            recycleScheduler.Cancel(this);
        }
    }
}
