using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalBallBallComboUseParticles : OriginalItemParticles
    {
        public ParticleSystem backGlowBg,backGlow;
        private ParticleSystemRenderer backRenderer,glowRenderer;
        protected override void Awake()
        { base.Awake(); backRenderer=backGlowBg.GetComponent<ParticleSystemRenderer>(); glowRenderer=backGlow.GetComponent<ParticleSystemRenderer>(); }
        public override int GetPoolId()=>33;
        public void Play() // 0x1f4384c.
        {
            int layer=SortingLayer.NameToID("Front");
            backRenderer.sortingLayerID=glowRenderer.sortingLayerID=layer; backRenderer.sortingOrder=98; glowRenderer.sortingOrder=99;
            backGlowBg.Play();backGlow.Play();
        }
    }
}
