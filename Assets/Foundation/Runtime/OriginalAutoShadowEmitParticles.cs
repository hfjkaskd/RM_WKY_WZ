using System;
using UnityEngine;

namespace Whitebox.Foundation
{
    // AutoShadowEmitParticles 0x2d743e0..0x2d74e8c. Each configured source is
    // cloned once on initialization and kept for all subsequent emissions.
    public sealed class OriginalAutoShadowEmitParticles : MonoBehaviour
    {
        public ParticleSystem system;
        public float alpha;
        public Vector3 offset;
        private ParticleSystem.Particle[] particles;
        private OriginalParticleShadowMath positionMath;

        public void Init()
        {
            var sourceTransform = system.transform;
            system = Instantiate(system);
            if (sourceTransform.parent != null)
            {
                system.transform.SetParent(sourceTransform.parent, true);
                system.transform.localScale = sourceTransform.localScale;
                system.transform.position = sourceTransform.position;
                system.transform.rotation = sourceTransform.rotation;
            }
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = unchecked(renderer.sortingOrder - 1);
            var emission = system.emission;
            int size;
            if (emission.burstCount < 1)
            {
                float rate = emission.rateOverTime.constant;
                if (rate <= 0f) size = 1;
                else
                {
                    rate = emission.rateOverTime.constant;
                    size = float.IsPositiveInfinity(rate) || rate <= int.MinValue ? int.MinValue :
                        float.IsNaN(rate) ? 0 : rate >= 2147483648f ? int.MaxValue : (int)rate;
                    if (size > 9) size = 10;
                    if (size < 2) size = 1;
                }
            }
            else
            {
                var burst = emission.GetBurst(0);
                size = Math.Max(burst.maxCount, burst.minCount);
            }
            particles = new ParticleSystem.Particle[unchecked(size * 10)];
        }
        public void EmitShadows(Vector3 position, int count)
        {
            position += offset;
            // Native rotation conversion precedes its scaling conversion.
            var angles = system.transform.eulerAngles;
            if (system.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                position = positionMath.ArrangeRotation(position, angles);
            if (system.main.simulationSpace != ParticleSystemSimulationSpace.World)
            {
                if (system.main.scalingMode == ParticleSystemScalingMode.Local)
                    position = positionMath.ArrangeScale(position, system.transform.localScale);
                if (system.main.scalingMode == ParticleSystemScalingMode.Hierarchy)
                    position = positionMath.ArrangeScale(position, system.transform.lossyScale);
            }
            var emit = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            system.Emit(emit, count);
            if (particles.Length < system.particleCount)
                particles = new ParticleSystem.Particle[unchecked(system.particleCount * 2)];
        }
        private void LateUpdate()
        {
            if (particles == null) return; // 0x2d74bb4; clones do not initialize again.
            int count = system.GetParticles(particles);
            for (int i = 0; i < count; i++)
                particles[i].startColor = OriginalParticleShadowMath.UpdateColor(particles[i].startColor, alpha);
            system.SetParticles(particles, count);
        }
        private void OnDestroy()
        {
            if (system != null && system.gameObject != null) Destroy(system.gameObject);
        }
    }
}
