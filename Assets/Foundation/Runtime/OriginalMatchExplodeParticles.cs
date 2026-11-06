using UnityEngine;

namespace Whitebox.Foundation
{
    // MatchItemExplodeParticles 0x21a8590..0x21a8eb8. The particle systems and
    // their modules are recovered prefab data; playback does not rebuild them.
    public sealed class OriginalMatchExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem lightRing;
        public ParticleSystem blastDots;
        public ParticleSystem[] blastParts;
        public OriginalAutoShadowEmitParticles[] shadowParticles;
        private int extraEmitCount = 4;

        protected override void Awake()
        {
            base.Awake();
            for (int i = 0; i < shadowParticles.Length; i++) shadowParticles[i].Init();
            extraEmitCount = 4;
        }
        public override int GetPoolId() => 4; // 0x21a8b80.
        public void Play(Vector3 position, bool extraEmit, int matchType) // 0x21a8618.
        {
            if (!blastDots.emission.enabled) return;
            int index = GetIndexByType(matchType);
            PrepareSeed(index);
            var centered = new ParticleSystem.EmitParams { position = position };
            var shaped = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            int count = extraEmitCount << (extraEmit ? 1 : 0);
            EnsureCapacity(lightRing, 1, count);
            EnsureCapacity(blastParts[index], count, count);
            // Native growth uses the part count, even for ten dots.
            EnsureCapacity(blastDots, 10, count);
            lightRing.Emit(centered, 1);
            shadowParticles[index].EmitShadows(position, count);
            blastParts[index].Emit(shaped, count);
            blastDots.Emit(shaped, 10);
        }
        public void PlayBlastParticles(Vector3 position, bool extraEmit, int matchType) // 0x21a8b88.
        {
            if (!blastDots.emission.enabled) return;
            int index = GetIndexByType(matchType);
            PrepareSeed(index);
            var shaped = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true };
            int count = extraEmitCount << (extraEmit ? 1 : 0);
            EnsureCapacity(blastParts[index], count, count);
            shadowParticles[index].EmitShadows(position, count);
            blastParts[index].Emit(shaped, count);
        }
        public static int GetIndexByType(int matchType) // 0x21a8b5c.
        {
            switch (matchType)
            {
                case 2: return 1;
                case 3: return 3;
                case 4: return 4;
                case 5: return 2;
                default: return 0;
            }
        }
        private void PrepareSeed(int index)
        {
            if (blastParts[index].isPlaying) return;
            float value = Random.Range(0f, 4294967296f);
            // The ARM64 signed/unsigned conversions saturate at the range ends.
            uint seed = value < 0f ? unchecked((uint)ToSigned(value)) : ToUnsigned(value);
            blastParts[index].randomSeed = seed;
            shadowParticles[index].system.randomSeed = seed;
        }
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
        private static void EnsureCapacity(ParticleSystem particles, int threshold, int count)
        {
            var main = particles.main;
            if (main.maxParticles <= unchecked(particles.particleCount + threshold))
            {
                main = particles.main;
                main.maxParticles = unchecked(particles.particleCount + count * 2);
            }
        }
    }
}
