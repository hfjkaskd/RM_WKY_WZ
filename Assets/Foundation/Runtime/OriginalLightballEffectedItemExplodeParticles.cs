using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballEffectedItemExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem[] particles;
        public Color[] particleColors;
        public override int GetPoolId() => 29; // 0x1f3f070.
        public void Play(Vector3 position, int matchType) // 0x1f3ea64.
        {
            var emit = new ParticleSystem.EmitParams { position = position, applyShapeToPosition = true,
                startColor = (Color32)particleColors[OriginalLightballViewAssets.GetColorIndex(matchType)] };
            for (int i = 0; i < particles.Length; i++)
            {
                var particle = particles[i];
                float count = particle.emission.GetBurst(0).count.constant;
                int amount = float.IsPositiveInfinity(count) ? int.MinValue : ToEmissionInt(count);
                var main = particle.main;
                if (main.maxParticles <= unchecked(particle.particleCount + amount))
                    main.maxParticles = unchecked(particle.particleCount + amount * 2);
                particle.Emit(emit, amount);
            }
        }
    }
}
