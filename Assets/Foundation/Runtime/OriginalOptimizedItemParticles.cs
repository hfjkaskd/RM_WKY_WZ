using System;
using System.Collections.Generic;
using UnityEngine;

namespace Whitebox.Foundation
{
    [Serializable]
    public struct OriginalParticleInfo
    {
        public ParticleSystem particle;
        public OriginalAutoShadowEmitParticles shadow;
    }

    [Serializable]
    public sealed class OriginalParticleInfoLayer
    {
        public OriginalParticleInfo[] particleInfos;
    }

    // OptimizedItemParticles 0x2b4e470..0x2b51c74. Systems, modules and layers
    // come from the recovered prefab, including its manual-emission settings.
    public abstract class OriginalOptimizedItemParticles : OriginalItemParticles
    {
        public OriginalParticleInfoLayer[] particleInfoLayers;
        protected Dictionary<Transform, Vector3> masterLastScales;
        protected Dictionary<uint, Transform> particleMasters;
        protected Dictionary<Transform, Vector3> masterLastPositions;
        protected List<DelayedEmitParticle> delayedEmitParticles;
        protected HashSet<uint> seenParticles;
        protected HashSet<Transform> seenMasters;
        private List<uint> tempViews;
        private List<Transform> tempLastScales, tempLastPositions;
        protected ParticleSystem.Particle[] tempParticles;
        private OriginalParticleShadowMath positionMath;
        // Store arithmetic boundaries explicitly: Mono may retain wider local
        // intermediates, while the recovered instructions operate on S registers.
        private float rateDuration, rateTime, rateAmount, rateDistance, rateY, rateZ, rateStep;
        protected virtual bool ShiftEmittedParticlesWithMaster => true;
        protected virtual float DeltaTime => OriginalLevelClock.QuantizeDelta(Time.deltaTime);

        protected override void Awake() // 0x2b4e478.
        {
            base.Awake();
            for (int layer = 0; layer < particleInfoLayers.Length; layer++)
            {
                var infos = particleInfoLayers[layer].particleInfos;
                for (int i = 0; i < infos.Length; i++)
                    if (infos[i].shadow != null) infos[i].shadow.Init();
            }
            masterLastScales = new Dictionary<Transform, Vector3>();
            particleMasters = new Dictionary<uint, Transform>();
            masterLastPositions = new Dictionary<Transform, Vector3>();
            delayedEmitParticles = new List<DelayedEmitParticle>();
            seenParticles = new HashSet<uint>();
            seenMasters = new HashSet<Transform>();
            tempViews = new List<uint>();
            tempLastScales = new List<Transform>();
            tempLastPositions = new List<Transform>();
            tempParticles = new ParticleSystem.Particle[200];
        }

        protected virtual void LateUpdate() // 0x2b4e86c.
        {
            seenParticles.Clear();
            seenMasters.Clear();
            for (int layer = 0; layer < particleInfoLayers.Length; layer++)
            {
                var infos = particleInfoLayers[layer].particleInfos;
                for (int i = 0; i < infos.Length; i++)
                {
                    var info = infos[i];
                    if (info.particle.particleCount == 0) continue;
                    ClearTempParticles(info.particle.particleCount);
                    UpdatePositions(info.particle, tempParticles);
                    if (info.shadow == null) continue;
                    ClearTempParticles(info.particle.particleCount);
                    UpdatePositions(info.shadow.system, tempParticles);
                }
            }
            UpdateLastPositions();
            ClearFinishedViews();
            CheckAndEmitDelayedEmitParticles();
            UpdateLastScales();
        }

        protected void ClearTempParticles(int count) // 0x2b4ea28/0x2b4fc04.
        {
            Array.Clear(tempParticles, 0, tempParticles.Length);
            if (tempParticles.Length < count) tempParticles = new ParticleSystem.Particle[unchecked(count * 2)];
        }

        protected void UpdatePositions(ParticleSystem system, ParticleSystem.Particle[] particles) // 0x2b4ea68.
        {
            int count = system.GetParticles(particles);
            if (system.particleCount <= 0) return;
            for (int i = 0; i < count; i++)
            {
                uint seed = tempParticles[i].randomSeed;
                if (!particleMasters.ContainsKey(seed)) continue;
                var master = particleMasters[seed];
                if (!seenParticles.Contains(seed)) seenParticles.Add(seed);
                if (!seenMasters.Contains(master)) seenMasters.Add(master);
                if (ShiftEmittedParticlesWithMaster && (master.gameObject == null || !master.gameObject.activeSelf))
                    particles[i].remainingLifetime = 0f;
                if (!ShiftEmittedParticlesWithMaster) continue;
                particles[i].position = (master.position - masterLastPositions[master]) + particles[i].position;
                if (system.main.scalingMode != ParticleSystemScalingMode.Hierarchy) continue;
                var scale = master.lossyScale;
                float lastX = masterLastScales[master].x;
                float lastY = masterLastScales[master].y;
                var size = particles[i].startSize3D;
                particles[i].startSize3D = new Vector3((scale.x / lastX) * size.x, (scale.y / lastY) * size.y, 1f);
            }
            system.SetParticles(particles, count);
        }

        private void UpdateLastPositions() // 0x2b4ee68.
        {
            tempLastPositions.Clear();
            foreach (var pair in masterLastPositions) tempLastPositions.Add(pair.Key);
            for (int i = 0; i < tempLastPositions.Count; i++)
            {
                var master = tempLastPositions[i];
                masterLastPositions[master] = master.transform.position;
            }
        }

        private void ClearFinishedViews() // 0x2b4f110.
        {
            tempViews.Clear();
            foreach (uint seed in particleMasters.Keys)
            {
                var master = particleMasters[seed];
                if (seenParticles.Contains(seed)) continue;
                if (!seenMasters.Contains(master))
                {
                    masterLastPositions.Remove(master);
                    masterLastScales.Remove(master);
                }
                tempViews.Add(seed);
            }
            for (int i = 0; i < tempViews.Count; i++) particleMasters.Remove(tempViews[i]);
        }

        private void UpdateLastScales() // 0x2b4f938.
        {
            tempLastScales.Clear();
            foreach (var pair in masterLastScales) tempLastScales.Add(pair.Key);
            for (int i = 0; i < tempLastScales.Count; i++)
            {
                var master = tempLastScales[i];
                masterLastScales[master] = master.transform.lossyScale;
            }
        }

        public void PlayOptimized(Vector3 position, Transform master, int layer = 0) // 0x2b4fc84.
        {
            if (!masterLastPositions.ContainsKey(master)) masterLastPositions.Add(master, master.position);
            if (!masterLastScales.ContainsKey(master)) masterLastScales.Add(master, master.lossyScale);
            PlayParticles(particleInfoLayers[layer].particleInfos, position, master);
        }

        public void PlayOptimized(Vector3 position, int layer = 0) // 0x2b502dc.
        {
            PlayParticles(particleInfoLayers[layer].particleInfos, position, null);
        }

        private void PlayParticles(OriginalParticleInfo[] infos, Vector3 position, Transform master) // 0x2b4fdcc.
        {
            var emit = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                var system = info.particle;
                emit.position = ArrangeScale(system, ArrangeRotation(system, position));
                if (!system.isPlaying)
                {
                    float value = UnityEngine.Random.Range(0f, 4294967296f);
                    uint seed = value < 0f ? unchecked((uint)ToSigned(value)) : ToUnsigned(value);
                    system.randomSeed = seed;
                    if (info.shadow != null) info.shadow.system.randomSeed = seed;
                }
                var offset = Vector3.zero;
                if (master != null)
                {
                    offset = (position - master.position) + system.transform.position;
                    emit.position = position + offset;
                }
                if (system.emission.burstCount > 0 && system.main.startDelay.constant <= 0f)
                    EmitBurstParticles(info, emit, master);
                if (system.main.startDelay.constant > 0f || system.main.loop ||
                    system.emission.rateOverTime.constant > 0f || system.emission.rateOverTime.curve != null)
                    delayedEmitParticles.Add(new DelayedEmitParticle(info, emit, offset, master));
            }
        }

        private Vector3 ArrangeRotation(ParticleSystem system, Vector3 position)
        {
            var angles = system.transform.eulerAngles;
            return system.main.simulationSpace == ParticleSystemSimulationSpace.Local ?
                positionMath.ArrangeRotation(position, angles) : position;
        }

        private Vector3 ArrangeScale(ParticleSystem system, Vector3 position)
        {
            if (system.main.simulationSpace == ParticleSystemSimulationSpace.World) return position;
            if (system.main.scalingMode == ParticleSystemScalingMode.Hierarchy)
                return positionMath.ArrangeScale(position, system.transform.lossyScale);
            if (system.main.scalingMode == ParticleSystemScalingMode.Local)
                return positionMath.ArrangeScale(position, system.transform.localScale);
            return position;
        }

        protected void EmitBurstParticles(OriginalParticleInfo info, ParticleSystem.EmitParams emit, Transform master) // 0x2b50318.
        {
            var burst = info.particle.emission.GetBurst(0);
            int amount = EmitAmount(burst.count.constant);
            if (burst.count.mode != ParticleSystemCurveMode.Constant)
                amount = UnityEngine.Random.Range(Math.Min(burst.maxCount, burst.minCount),
                    unchecked(Math.Max(burst.maxCount, burst.minCount) + 1));
            if (amount > 0) EmitParticles(info, emit, master, amount);
        }

        protected void EmitParticles(OriginalParticleInfo info, ParticleSystem.EmitParams emit, Transform master, int amount) // 0x2b50648.
        {
            CheckAndUpdateParticleMaxSize(info.particle, amount, info.shadow);
            int previousCount = info.particle.particleCount;
            info.particle.Emit(emit, amount);
            UpdateMasterInfos(info.particle, master, amount, previousCount);
            if (info.shadow != null) info.shadow.EmitShadows(emit.position, amount);
        }

        private static void CheckAndUpdateParticleMaxSize(ParticleSystem system, int size, OriginalAutoShadowEmitParticles shadow) // 0x2b50cf4.
        {
            var main = system.main;
            if (main.maxParticles > unchecked(system.particleCount + size)) return;
            main.maxParticles = unchecked(system.particleCount + size * 2);
            if (shadow == null) return;
            main = shadow.system.main;
            main.maxParticles = unchecked(shadow.system.particleCount + size * 2);
        }

        private void UpdateMasterInfos(ParticleSystem system, Transform master, int amount, int previousCount) // 0x2b50e20.
        {
            if (master == null) return;
            int count = unchecked(previousCount + amount);
            ClearTempParticles(count);
            system.GetParticles(tempParticles);
            bool changed = false;
            for (int i = previousCount; i < count; i++)
            {
                uint oldSeed = tempParticles[i].randomSeed;
                uint seed = GetParticleRandomSeed(oldSeed);
                tempParticles[i].randomSeed = seed;
                particleMasters.Add(tempParticles[i].randomSeed, master);
                changed |= seed != oldSeed;
            }
            if (changed) system.SetParticles(tempParticles, count);
            if (!masterLastPositions.ContainsKey(master)) masterLastPositions.Add(master, master.transform.position);
            if (!masterLastScales.ContainsKey(master)) masterLastScales.Add(master, master.transform.lossyScale);
        }

        protected uint GetParticleRandomSeed(uint seed) // 0x2b51128.
        {
            while (particleMasters.ContainsKey(seed))
            {
                float value = UnityEngine.Random.Range(0f, 4294967296f);
                seed = value < 0f ? unchecked((uint)ToSigned(value)) : ToUnsigned(value);
            }
            return seed;
        }

        protected Vector3 SetStartSize3D(ParticleSystem.MainModule main, Transform master, DelayedEmitParticle entry) // 0x2b50764.
        {
            if (main.scalingMode != ParticleSystemScalingMode.Hierarchy) return Vector3.one;
            var scale = master.lossyScale;
            float lastX = masterLastScales[master].x;
            float lastY = masterLastScales[master].y;
            var size = Vector3.zero;
            if (main.startSize.mode == ParticleSystemCurveMode.Constant)
            {
                size.x = main.startSizeX.constant;
                size.y = main.startSizeY.constant;
            }
            else if (main.startSize.mode == ParticleSystemCurveMode.TwoConstants)
            {
                size.x = UnityEngine.Random.Range(main.startSizeX.constantMin, main.startSizeX.constantMax);
                size.y = UnityEngine.Random.Range(main.startSizeY.constantMin, main.startSizeY.constantMax);
            }
            entry.scaleMultiplier = new Vector3((scale.x / lastX) * entry.scaleMultiplier.x,
                (scale.y / lastY) * entry.scaleMultiplier.y, 0f);
            return new Vector3(size.x, size.y, 0f);
        }

        protected void CheckAndEmitDelayedEmitParticles() // 0x2b4f4ac.
        {
            int count = delayedEmitParticles.Count;
            float delta = DeltaTime;
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                var entry = delayedEmitParticles[index];
                var system = entry.info.particle;
                var master = entry.master;
                if (master != null && (master.gameObject == null || !master.gameObject.activeInHierarchy))
                {
                    delayedEmitParticles.RemoveAt(index);
                    continue;
                }
                var size = SetStartSize3D(system.main, master, entry);
                if (master != null)
                {
                    var position = ArrangeScale(system, ArrangeRotation(system, master.position + entry.positionOffset));
                    var previous = entry.emit.position;
                    entry.emit.position = position;
                    entry.velocity = (position - previous) / DeltaTime;
                    if (system.main.scalingMode == ParticleSystemScalingMode.Hierarchy)
                        entry.emit.startSize3D = new Vector3(size.x * entry.scaleMultiplier.x, size.y * entry.scaleMultiplier.y, 0f);
                }
                bool emitRate = entry.startRemainingTime <= 0f;
                if (!emitRate)
                {
                    entry.startRemainingTime -= delta;
                    if (entry.startRemainingTime <= 0f && system.emission.burstCount > 0)
                    {
                        EmitBurstParticles(entry.info, entry.emit, master);
                        emitRate = true;
                    }
                }
                if (emitRate)
                {
                    entry.remainingTime -= delta;
                    if (entry.remainingTime <= 0f)
                    {
                        if (!system.main.loop)
                        {
                            delayedEmitParticles.RemoveAt(index);
                            continue;
                        }
                        entry.remainingTime += system.main.duration;
                        if (system.emission.burstCount > 0) EmitBurstParticles(entry.info, entry.emit, master);
                    }
                    if (system.emission.rateOverTime.constant > 0f || system.emission.rateOverTime.curve != null)
                        EmitRateOverTimeAndDistanceParticles(entry);
                }
                index++;
            }
        }

        protected void EmitRateOverTimeAndDistanceParticles(DelayedEmitParticle entry) // 0x2b50a10.
        {
            var system = entry.info.particle;
            rateDuration = system.main.duration;
            rateTime = rateDuration - entry.remainingTime;
            rateTime /= rateDuration;
            rateAmount = 0f;
            var emission = system.emission;
            if (emission.rateOverTime.constant > 0f || emission.rateOverTime.curve != null)
                rateAmount = emission.rateOverTime.Evaluate(rateTime) * DeltaTime;
            if (emission.rateOverDistance.constant > 0f || emission.rateOverDistance.curve != null)
            {
                rateDistance = entry.velocity.x * entry.velocity.x;
                rateY = entry.velocity.y * entry.velocity.y;
                rateZ = entry.velocity.z * entry.velocity.z;
                rateDistance += rateY;
                rateDistance += rateZ;
                rateDistance = (float)Math.Sqrt(rateDistance);
                rateStep = emission.rateOverDistance.Evaluate(rateTime) * DeltaTime;
                rateStep *= rateDistance;
                rateAmount += rateStep;
            }
            rateAmount += entry.emitAccumulator;
            int emitCount = EmitAmount(rateAmount);
            entry.emitAccumulator = rateAmount;
            if (emitCount <= 0) return;
            entry.emitAccumulator -= emitCount;
            EmitParticles(entry.info, entry.emit, entry.master, emitCount);
        }

        public void StopParticles(Transform master, int layer = 0, bool clearEmitted = false) // 0x2b511c0.
        {
            if (master == null) return;
            var infos = particleInfoLayers[layer].particleInfos;
            if (clearEmitted) ClearEmittedParticles(infos, master);
            int count = delayedEmitParticles.Count;
            int index = 0;
            for (int i = 0; i < count; i++)
            {
                if (delayedEmitParticles[index].master == master) delayedEmitParticles.RemoveAt(index);
                else index++;
            }
        }

        private void ClearEmittedParticles(OriginalParticleInfo[] infos, Transform master) // 0x2b51338.
        {
            for (int i = 0; i < infos.Length; i++)
            {
                var info = infos[i];
                int count = info.particle.particleCount;
                bool hasShadow = info.shadow != null;
                ClearCorrespondingParticles(master, count, info.particle, !hasShadow);
                if (hasShadow) ClearCorrespondingParticles(master, count, info.shadow.system, true);
            }
        }

        private void ClearCorrespondingParticles(Transform master, int capacity, ParticleSystem system, bool clearList) // 0x2b518b4.
        {
            ClearTempParticles(capacity);
            int count = system.GetParticles(tempParticles);
            for (int i = 0; i < count; i++)
            {
                uint seed = tempParticles[i].randomSeed;
                if (!particleMasters.ContainsKey(seed) || particleMasters[seed] != master) continue;
                tempParticles[i].remainingLifetime = 0f;
                if (clearList) particleMasters.Remove(seed);
            }
            system.SetParticles(tempParticles, count);
        }

        public List<List<ParticleSystem.Particle>> GetParticles(Transform master, int layer = 0) // 0x2b51474.
        {
            if (master == null) return null;
            var infos = particleInfoLayers[layer].particleInfos;
            var result = new List<List<ParticleSystem.Particle>>(infos.Length);
            for (int i = 0; i < infos.Length; i++)
            {
                var system = infos[i].particle;
                ClearTempParticles(system.particleCount);
                int count = system.GetParticles(tempParticles);
                var particles = new List<ParticleSystem.Particle>();
                for (int j = 0; j < count; j++)
                {
                    uint seed = tempParticles[j].randomSeed;
                    if (particleMasters.ContainsKey(seed) && particleMasters[seed] == master) particles.Add(tempParticles[j]);
                }
                result.Add(particles);
            }
            return result;
        }

        public override void OnRecycle() // 0x2b51ac8. Native leaves engine particles intact.
        {
            base.OnRecycle();
            masterLastPositions.Clear(); masterLastScales.Clear(); particleMasters.Clear(); delayedEmitParticles.Clear();
            Array.Clear(tempParticles, 0, tempParticles.Length);
            seenMasters.Clear(); seenParticles.Clear(); tempLastPositions.Clear(); tempLastScales.Clear(); tempViews.Clear();
        }

        private static int EmitAmount(float value) => float.IsPositiveInfinity(value) ? int.MinValue : ToSigned(value);
        private static int ToSigned(float value)
        {
            if (float.IsNaN(value)) return 0;
            if (value <= int.MinValue) return int.MinValue;
            if (value >= 2147483648f) return int.MaxValue;
            return (int)value;
        }
        private static uint ToUnsigned(float value)
        {
            if (float.IsNaN(value) || value <= 0f) return 0;
            if (value >= 4294967296f) return uint.MaxValue;
            return (uint)value;
        }

        protected sealed class DelayedEmitParticle // 0x2b504cc.
        {
            public readonly OriginalParticleInfo info;
            public ParticleSystem.EmitParams emit;
            public readonly Vector3 positionOffset;
            public Vector3 scaleMultiplier = Vector3.one;
            public readonly Transform master;
            public float remainingTime, startRemainingTime, emitAccumulator;
            public Vector3 velocity = Vector3.zero;
            public DelayedEmitParticle(OriginalParticleInfo info, ParticleSystem.EmitParams emit, Vector3 offset, Transform master)
            {
                this.info = info; this.emit = emit; positionOffset = offset; this.master = master;
                remainingTime = info.particle.main.duration;
                startRemainingTime = info.particle.main.startDelay.constant;
            }
        }
    }
}
