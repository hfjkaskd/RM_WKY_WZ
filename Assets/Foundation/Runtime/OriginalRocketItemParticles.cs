using UnityEngine;
using UnityEngine.Rendering;

namespace Whitebox.Foundation
{
    public sealed class OriginalRocketItemParticles : OriginalItemParticles
    {
        public ParticleSystem circleParticle;
        public SortingGroup sortingGroup;
        public float positionOffset;
        public int itemSortingOrder;
        private int itemLayer;
        private OriginalGameSceneProjection projection;
        protected override void Awake() { base.Awake(); itemLayer = SortingLayer.NameToID("Item"); }
        public override int GetPoolId() => 9; // 0x27bb780.
        public override void OnSpawn() // 0x27bb6ec / 0x12d8588.
        {
            base.OnSpawn(); sortingGroup.sortingLayerID = itemLayer; sortingGroup.sortingOrder = itemSortingOrder;
        }
        // Both ordinary flight strategies restart their active trails on the
        // camera orientation event (0x27bd190 / 0x27be648).
        internal void BindOrientation(OriginalGameSceneProjection value)
        {
            projection = value;
            projection.OnOrientationChanged += StopAndPlay;
        }
        public override void OnRecycle()
        {
            ReleaseOrientation();
            base.OnRecycle();
        }
        private void OnDestroy() => ReleaseOrientation();
        private void ReleaseOrientation()
        {
            if (projection == null) return;
            projection.OnOrientationChanged -= StopAndPlay;
            projection = null;
        }

        public void SetParticlePosition(Vector3 direction) => circleParticle.transform.localPosition = direction * positionOffset; // 0x27bb788.
        public void Play() => circleParticle.Play(); // 0x27bb764.
        public void StopAndPlay() // 0x27bb7e0.
        { circleParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); circleParticle.Play(); }
    }
}
