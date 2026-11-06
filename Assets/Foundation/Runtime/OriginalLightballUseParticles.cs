using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalLightballUseParticles : OriginalItemParticles
    {
        public ParticleSystem backGlowBg, backGlow, ring;
        private ParticleSystemRenderer backRenderer, glowRenderer, ringRenderer;
        protected override void Awake()
        {
            base.Awake();
            backRenderer = backGlowBg.GetComponent<ParticleSystemRenderer>();
            glowRenderer = backGlow.GetComponent<ParticleSystemRenderer>();
            ringRenderer = ring.GetComponent<ParticleSystemRenderer>();
        }
        public override int GetPoolId() => 30; // 0x1f42718.
        public void Play(int itemLayer) // 0x1f425bc / GetLightballUseSorting(0) 0x12d9148.
        {
            backRenderer.sortingLayerID = glowRenderer.sortingLayerID = ringRenderer.sortingLayerID = itemLayer;
            backRenderer.sortingOrder = 11008;
            glowRenderer.sortingOrder = ringRenderer.sortingOrder = 11009;
            backGlowBg.Play(); backGlow.Play(); ring.Play();
        }
        public void StopEmitting() { RecycleSelf(); } // 0x1f4270c dispatches ItemParticles.RecycleSelf (slot8).
    }
}
