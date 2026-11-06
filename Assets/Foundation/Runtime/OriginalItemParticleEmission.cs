using System;
using System.Collections;
using UnityEngine;

namespace Whitebox.Foundation
{
    public abstract partial class OriginalItemParticles
    {
        private OriginalParticleShadowMath emissionPositionMath;
        // ItemParticles.PlayParticles 0x2b48aec. This is the ordinary emitter;
        // it uses the original shared wait object for delayed bursts.
        protected void PlayParticles(OriginalParticleInfo[] infos, Vector3 position)
        {
            var emit = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var system = info.particle;
                if (!system.isPlaying)
                {
                    float value = UnityEngine.Random.Range(0f, 4294967296f);
                    uint seed = value < 0f ? unchecked((uint)ToEmissionInt(value)) : ToEmissionUInt(value);
                    system.randomSeed = seed;
                    if (info.shadow != null) info.shadow.system.randomSeed = seed;
                }
                var emission = system.emission;
                if (emission.burstCount < 1) continue;
                var burst = emission.GetBurst(0);
                int amount = float.IsPositiveInfinity(burst.count.constant) ? int.MinValue : ToEmissionInt(burst.count.constant);
                Vector3 arranged = position;
                if (system.main.simulationSpace == ParticleSystemSimulationSpace.Local)
                    arranged = emissionPositionMath.ArrangeRotation(arranged, system.transform.eulerAngles);
                if (system.main.simulationSpace != ParticleSystemSimulationSpace.World)
                {
                    if (system.main.scalingMode == ParticleSystemScalingMode.Hierarchy)
                        arranged = emissionPositionMath.ArrangeScale(arranged, system.transform.lossyScale);
                    else if (system.main.scalingMode == ParticleSystemScalingMode.Local)
                        arranged = emissionPositionMath.ArrangeScale(arranged, system.transform.localScale);
                }
                emit.position = arranged;
                if (burst.count.mode != ParticleSystemCurveMode.Constant)
                    amount = UnityEngine.Random.Range(Math.Min(burst.maxCount, burst.minCount),
                        unchecked(Math.Max(burst.maxCount, burst.minCount) + 1));
                var main = system.main;
                if (main.maxParticles <= unchecked(system.particleCount + amount))
                {
                    main = system.main;
                    main.maxParticles = unchecked(system.particleCount + amount * 2);
                    if (info.shadow != null)
                    {
                        var shadowMain = info.shadow.system.main;
                        shadowMain.maxParticles = unchecked(info.shadow.system.particleCount + amount * 2);
                    }
                }
                // Native b.le includes unordered values (NaN).
                if (!(system.main.startDelay.constant > 0f))
                {
                    system.Emit(emit, amount);
                    if (info.shadow != null) info.shadow.EmitShadows(position, amount);
                }
                else StartCoroutine(StartEmit(info, emit, position, amount));
            }
        }
        private IEnumerator StartEmit(OriginalParticleInfo info, ParticleSystem.EmitParams emit, Vector3 position, int amount)
        {
            float count = info.particle.main.startDelay.constant / .025f;
            int wait = float.IsPositiveInfinity(count) ? int.MinValue : ToEmissionInt(count);
            for (int i = 0; i < wait; i++) yield return waitDuration;
            info.particle.Emit(emit, amount);
            if (info.shadow != null) info.shadow.EmitShadows(position, amount);
        }
        protected static void InitShadows(OriginalParticleInfo[] infos) // 0x2b493a8.
        {
            for (int i = 0; i < infos.Length; i++) if (infos[i].shadow != null) infos[i].shadow.Init();
        }
        protected static int ToEmissionInt(float value)
        {
            if (float.IsNaN(value)) return 0;
            if (value <= int.MinValue) return int.MinValue;
            if (value >= 2147483648f) return int.MaxValue;
            return (int)value;
        }
        private static uint ToEmissionUInt(float value)
        {
            if (float.IsNaN(value) || value <= 0f) return 0;
            if (value >= 4294967296f) return uint.MaxValue;
            return (uint)value;
        }
    }
}
