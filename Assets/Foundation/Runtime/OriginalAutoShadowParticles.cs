using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // AutoShadowParticles0x2d75120/2d75234/2d754e0: duplicate into the same
    // particle system, then fade the first half from transparent black.
    public sealed class OriginalAutoShadowParticles : MonoBehaviour
    {
        public ParticleSystem system;
        public float alpha;
        public Vector3 offset;
        private bool shouldUpdate;
        private int updateShadowCount;
        private ParticleSystem.Particle[] particles;
        private void Awake()
        {
            var emission = system.emission;
            if (alpha <= 0 || emission.burstCount < 1) return;
            shouldUpdate = true;
            var burst = emission.GetBurst(0);
            particles = new ParticleSystem.Particle[Math.Max(burst.maxCount, burst.minCount) * 4];
        }

        private void LateUpdate()
        {
            if (!shouldUpdate) return;
            int count = system.GetParticles(particles);
            if (updateShadowCount == 0)
            {
                if (count == 0) return;
                for (int i = 0; i < count; i++)
                {
                    particles[count + i] = particles[i];
                    particles[i].startColor = new Color32(0, 0, 0, 0);
                    particles[i].position += offset;
                }
                system.SetParticles(particles, count * 2);
            }
            else
            {
                float scaled = Mathf.Clamp01(alpha * updateShadowCount) * 255f;
                byte opacity = (byte)Mathf.RoundToInt(scaled);
                for (int i = 0; i < count / 2; i++) particles[i].startColor = new Color32(0, 0, 0, opacity);
                system.SetParticles(particles, count);
            }
            updateShadowCount++;
            if (updateShadowCount >= 11) shouldUpdate = false;
        }

        private void OnDisable()
        {
            if (alpha > 0 && system.emission.burstCount > 0) shouldUpdate = true;
            updateShadowCount = 0;
        }
    }
}
