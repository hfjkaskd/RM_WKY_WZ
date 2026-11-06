using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    // SpecialItemCreationParticlesBack 0x21aa9c8..0x21aad5c.
    public sealed class OriginalSpecialItemCreationParticlesBack : OriginalItemParticles
    {
        public ParticleSystem[] particleSystems;
        private Transform target;
        private Vector3 lastPosition;
        private OriginalParticleRecycleScheduler recycleScheduler;
        protected override void Awake()
        {
            base.Awake();
            recycleScheduler = OriginalParticleContext.RecycleScheduler;
        }
        public override int GetPoolId() => 6;
        public void Init(Transform master)
        {
            target = master;
            lastPosition = master.position;
        }
        private void Update()
        {
            if (target == null) return;
            var position = target.position;
            transform.position += position - lastPosition;
            lastPosition = position;
        }
        public void PlayParticles(List<List<ParticleSystem.Particle>> particles)
        {
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var system = particleSystems[i];
                system.Play(false);
                var copied = particles[i].ToArray();
                system.SetParticles(copied, particles[i].Count);
                system.Stop(false);
            }
            // Native uses Invoke with a one-second scaled-time deadline.
            // No string method names are retained under the Obfuz requirement.
            recycleScheduler.Schedule(this, Time.time, 1f);
        }
        public override void CancelScheduledActions()
        {
            base.CancelScheduledActions();
            recycleScheduler.Cancel(this);
        }
        public override void OnRecycle()
        {
            base.OnRecycle();
            transform.position = Vector3.zero;
            target = null;
        }
    }
}
