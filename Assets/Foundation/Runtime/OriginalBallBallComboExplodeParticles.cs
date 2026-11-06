using UnityEngine;

namespace Whitebox.Foundation
{
    public sealed class OriginalBallBallComboExplodeParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle;
        private Renderer[] renderers;
        protected override void Awake() { base.Awake(); renderers=GetComponentsInChildren<Renderer>(); }
        public override int GetPoolId()=>34;
        public void Play(bool isBack) // 0x1f43654 / 0x1f436c8.
        {
            int layer=SortingLayer.NameToID("Front"),order=isBack?90:501;
            for(int i=0;i<renderers.Length;i++) { renderers[i].sortingLayerID=layer;renderers[i].sortingOrder=order+i; }
            circleParticle.Play(); OriginalParticleContext.RecycleScheduler.Schedule(this,Time.time,2f);
        }
    }
}
