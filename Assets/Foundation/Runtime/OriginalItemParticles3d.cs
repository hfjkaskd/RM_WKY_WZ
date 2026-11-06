using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalItemParticles3d : MonoBehaviour
    {
        private static readonly int shaderColor = Shader.PropertyToID("_Color");
        [SerializeField] private OriginalSortedParticleSystem3d particle, shadow;
        [SerializeField] private AnimationCurve shadowOffsetX, shadowOffsetY, shadowAlpha;
        private MaterialPropertyBlock properties;
        private OriginalSortedParticleDepth depth;
        private bool played;
        private float elapsed;

        private void Awake() => properties = new MaterialPropertyBlock();

        public void Play(OriginalSortedParticleDepth depth)
        {
            this.depth = depth;
            elapsed = 0f;
            particle.ParticleSystem.Play();
            depth.Add(particle);
            if (shadow == null) return;
            shadow.transform.position = particle.transform.position;
            depth.Add(shadow);
            shadow.ParticleSystem.randomSeed = particle.ParticleSystem.randomSeed;
            shadow.ParticleSystem.Play();
            played = true;
        }

        private void Update() // ItemParticles3d.Update 0x2b498cc.
        {
            if (!played) return;
            properties.SetColor(shaderColor, new Color(0f, 0f, 0f, shadowAlpha.Evaluate(elapsed)));
            shadow.Renderer.SetPropertyBlock(properties);
            var point = particle.transform.position;
            point.x += shadowOffsetX.Evaluate(elapsed);
            point.y += shadowOffsetY.Evaluate(elapsed);
            point.z = shadow.transform.position.z;
            shadow.transform.position = point;
            elapsed += Time.deltaTime;
        }

        private void OnDisable()
        {
            played = false;
            if (depth == null) return;
            depth.Remove(particle);
            if (shadow != null) depth.Remove(shadow);
            depth = null;
        }
    }
}
