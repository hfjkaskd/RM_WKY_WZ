using System.Collections.Generic;

namespace Whitebox.Foundation
{
    // ParticlesManager 0x26912f0/0x1edaa5c/0x1edab0c/0x1edab64.
    public sealed class OriginalParticlesManager
    {
        private readonly OriginalGameObjectPool pool;
        private readonly List<OriginalItemParticles> active = new List<OriginalItemParticles>();
        private readonly Dictionary<int, OriginalItemParticles> globals = new Dictionary<int, OriginalItemParticles>();
        public int ActiveCount => active.Count;
        public OriginalParticlesManager(OriginalGameObjectPool pool) { this.pool = pool; }
        public T GetGlobalParticle<T>(int id) where T : OriginalItemParticles
        {
            globals.TryGetValue(id, out var instance);
            if (instance == null)
            {
                instance = pool.Spawn<OriginalItemParticles>(id, true);
                // Native Add must surface a stale destroyed entry, not replace it.
                globals.Add(id, instance);
            }
            return (T)instance;
        }
        public void ParticlesSpawned(OriginalItemParticles particles) { active.Add(particles); }
        public void ParticlesRecycled(OriginalItemParticles particles) { active.Remove(particles); }
        public void Recycle(OriginalItemParticles particles) { pool.Recycle(particles); }
        internal void CancelScheduledActions()
        {
            foreach (var particles in active) particles.CancelScheduledActions();
        }
        public void Clear(bool gameExit)
        {
            // Callbacks remove entries from active, so traversal is backwards.
            // The native gameExit parameter does not alter this implementation.
            for (int i = active.Count - 1; i >= 0; i--)
            {
                var particles = active[i];
                if (!particles.gameObject.activeSelf) continue;
                particles.CancelScheduledActions();
                particles.RecycleSelf();
            }
            globals.Clear();
        }
    }
}
