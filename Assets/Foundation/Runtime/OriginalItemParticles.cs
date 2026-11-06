using UnityEngine;

namespace Whitebox.Foundation
{
    // Strongly typed portion of LevelContext used by ItemParticles.Awake.
    public static class OriginalParticleContext
    {
        public static OriginalParticlesManager Manager { get; set; }
        public static OriginalParticleRecycleScheduler RecycleScheduler { get; set; }
    }

    public abstract partial class OriginalItemParticles : MonoBehaviour, IOriginalPoolable
    {
        private OriginalParticlesManager manager;
        protected readonly WaitForSeconds waitDuration = new WaitForSeconds(.015f);
        protected virtual void Awake() { manager = OriginalParticleContext.Manager; } // 0x2b489c8.
        public abstract int GetPoolId();
        public virtual void OnSpawn() { manager.ParticlesSpawned(this); } // 0x2b48aac.
        public virtual void OnRecycle() { manager.ParticlesRecycled(this); } // 0x2b48acc.
        public virtual void RecycleSelf() { manager.Recycle(this); } // 0x2b48a58.
        public virtual void CancelScheduledActions() { CancelInvoke(); }
    }
}
